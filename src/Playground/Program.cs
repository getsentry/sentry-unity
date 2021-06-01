using System;

namespace Playground
{
    class A
    {
        public virtual void Foo()
        {
            Console.WriteLine("A");
        }
    }

    class B : A
    {
        public override void Foo()
        {
            Console.WriteLine("B");
        }
    }

    class C : A
    {
        public new void Foo()
        {
            Console.WriteLine("C");
        }
    }

    static class Program
    {
        static void Main(string[] args)
        {
            A a = new A();
            A ab = new B();
            A ac = new C();

            a.Foo();
            ab.Foo();
            ac.Foo();

            C c = new C();
            c.Foo();
            B b = new B();
            b.Foo();
        }
    }
}
