﻿namespace Funcky.Test
{
    class SideEffect
    {
        public void Do() => IsDone = true;

        public bool IsDone { get; private set; }
    }
}