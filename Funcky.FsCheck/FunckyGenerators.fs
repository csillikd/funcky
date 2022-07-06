namespace Funcky.FsCheck

open System
open System.Collections.Generic
open FsCheck
open Funcky.Monads
open Microsoft.FSharp.Core

type EquatableException =
    inherit Exception

    new (message) = { inherit Exception(message) }

    interface IEquatable<EquatableException> with
        member this.Equals(other) = other.Message = this.Message

    override this.Equals(other) =
        match other with
        | :? EquatableException as o -> this.Message = o.Message
        | _ -> false

    override this.GetHashCode() = this.Message.GetHashCode()

[<Sealed>]
[<AbstractClass>]
type FunckyGenerators =
    static member either<'a, 'b>() =
        (Arb.fromGen << Gen.oneof) [
            Arb.generate<'a> |> Gen.map Either<'a, 'b>.Left
            Arb.generate<'b> |> Gen.map Either<'a, 'b>.Right]

    static member result<'a>() =
        (Arb.fromGen << Gen.oneof) [
            Arb.generate<'a> |> Gen.map Result.Ok
            Arb.generate<string> |> Gen.map (EquatableException >> Result<'a>.Error)]

    static member tuple2<'a, 'b>() =
       Arb.fromGen <|
           gen { let! value1 = Arb.generate<'a>
                 let! value2 = Arb.generate<'b>
                 return ValueTuple.Create(value1, value2) }

#if PRIORITY_QUEUE
    static member priorityQueue<'a, 'priority>() =
         Arb.fromGen <|
             gen { let! values = Arb.generate<List<ValueTuple<'a, 'priority>>>
                   return PriorityQueue(values) }
#endif

    static member option<'a>() =
        { new Arbitrary<Funcky.Monads.Option<'a>>() with
            override _.Generator =
                Gen.frequency [(1, gen { return Option<'a>.None }); (7, Arb.generate |> Gen.filter FunckyGenerators.isNotNull |> Gen.map Option.Some)]
            override _.Shrinker o =
                o.Match(none = Seq.empty, some = fun x -> seq { yield Option<'a>.None; for x' in Arb.shrink x -> Option.Some x' })
        }

    static member generateLazy<'a>() =
        Arb.fromGen (Arb.generate<'a> |> Gen.map Lazy.Return)

    static member generateReader<'env, 'a>() =
        Arb.fromGen (Arb.generate<Func<'env, 'a>> |> Gen.map Reader<'env>.FromFunc)

    [<CompiledName("Register")>]

    static member register() = Arb.registerByType typeof<FunckyGenerators> |> ignore

    static member private isNotNull x = not (Object.ReferenceEquals(x, null))
