using System;
using Collections.Pooled;

[Test]
public unsafe static class UnsafeTest
{
    public static void Run()
    {
        Re.GOAP.FSM.Run();
    }
}

namespace Re.GOAP
{
    public enum States
    {
        Idle, Run, Dead,
    }

    public unsafe interface IState
    {
        int Holder { get; set; }
        void Play();
    }

    public unsafe struct Idle : IState
    {
        public int Holder { get; set; }

        public void Play() => Console.WriteLine($"[Holder: {Holder}] Playing `{nameof(Idle)}` state");
    }

    public unsafe struct Run : IState
    {
        public int Holder { get; set; }

        public void Play() => Console.WriteLine($"[Holder: {Holder}] Playing `{nameof(Run)}` state");
    }

    public unsafe struct Dead : IState
    {
        public int Holder { get; set; }

        public void Play() => Console.WriteLine($"[Holder: {Holder}] Playing `{nameof(Dead)}` state");
    }

    public unsafe static class FSM
    {
        public static void* State { get => m_State; }
        public static States StateKind { get => m_StateKind; set => m_StateKind = value; }

        private static void* m_State;
        private static States m_StateKind;
        private static readonly PooledDictionary<IntPtr, int> m_Dict = new PooledDictionary<IntPtr, int>();

        public static void Run()
        {
            var idleState = new Idle()
            {
                Holder = 777
            };
            m_State = &idleState;
            m_StateKind = States.Idle;

            ((Idle*)m_State)->Play();
            Console.WriteLine(m_StateKind);
        }
    }
}
