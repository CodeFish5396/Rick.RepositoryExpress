using System;

namespace Rick.RepositoryExpress.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            DerivedDemo.CurrentIndex = 5;
            DerivedDemo derivedDemo = new DerivedDemo();
            BaseDemo baseDemo = (BaseDemo)derivedDemo;

            Console.WriteLine(derivedDemo.Name);
            Console.WriteLine(baseDemo.Name);
            Console.ReadKey();
        }
    }
}
