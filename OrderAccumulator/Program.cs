using System;
using QuickFix;

namespace OrderAccumulator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("=============");
            Console.WriteLine();
            Console.WriteLine("                                                    ! ! !");
            Console.WriteLine("                                 Desafio ATG Alexandre Borges.");
            Console.WriteLine("                                    Acumulador de pedidos     ");
            Console.WriteLine("                                                    ! ! !");
            Console.WriteLine();
            Console.WriteLine("=============");

            try
            {
                SessionSettings settings = new SessionSettings(@"simpleacc.cfg");
                IApplication app = new Accumulator();
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                ILogFactory logFactory = new FileLogFactory(settings);
                DefaultMessageFactory messageFactory = new DefaultMessageFactory();
                IAcceptor acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory, messageFactory);

                acceptor.Start();
                Console.WriteLine("press <enter> to quit");
                Console.Read();
                acceptor.Stop();
            }
            catch (System.Exception e)
            {
                Console.WriteLine("==FATAL ERROR==");
                Console.WriteLine(e.ToString());
            }
        }
    }
}