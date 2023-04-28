using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Linq;

public class Accumulator : MessageCracker, IApplication
{
    private readonly Dictionary<string, List<QuickFix.FIX42.NewOrderSingle>> _orders = new Dictionary<string, List<QuickFix.FIX42.NewOrderSingle>>();
    private readonly Dictionary<string, decimal> _exposures = new Dictionary<string, decimal>();
    private const decimal ExposureLimit = 1000000m;
    private Session _session;

    public void OnCreate(SessionID sessionID)
    {
        _session = Session.LookupSession(sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        Console.WriteLine("Logged in to session: " + sessionID);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        Crack(message, sessionID);
    }

    public void ToApp(Message message, SessionID sessionID)
    {
    }

    public void OnLogout(SessionID sessionID)
    {
        Console.WriteLine("Logged out from session: " + sessionID);
    }

    public void OnMessage(QuickFix.FIX42.NewOrderSingle order, SessionID sessionID)
    {
        Console.WriteLine("Order Received: OrderID=" + order.ClOrdID + " Symbol=" + order.Symbol.getValue() +
            " Side=" + order.Side.getValue() + " Quantity=" + order.OrderQty.getValue() + " Price=" + order.Price.getValue());

        if (!_orders.ContainsKey(order.Symbol.getValue()))
        {
            _orders[order.Symbol.getValue()] = new List<QuickFix.FIX42.NewOrderSingle>();
            _exposures[order.Symbol.getValue()] = 0;
        }

        if (order.OrderQty.getValue() * order.Price.getValue() > 1000)
        {
            // Reject order
            var reject = new QuickFix.FIX42.OrderCancelReject(
                new OrderID(order.ClOrdID.getValue()),
                new ClOrdID(order.ClOrdID.getValue()),
                new OrigClOrdID(order.ClOrdID.getValue()),
                new OrdStatus('8'),
                new CxlRejResponseTo('1')
            );
            Session.SendToTarget(reject, _session.SessionID);
            Console.WriteLine(_exposures[order.Symbol.getValue()]);
        }
        else
        {
            // Accept order and send ExecutionReport
            var execution = new QuickFix.FIX42.ExecutionReport(
                new OrderID(order.ClOrdID.getValue()),
                new ExecID(Guid.NewGuid().ToString()),
                new ExecTransType(ExecTransType.NEW),
                new ExecType(ExecType.FILL),
                new OrdStatus(OrdStatus.FILLED),
                order.Symbol,
                order.Side,
                new LeavesQty(0),
                new CumQty(order.OrderQty.getValue()),
                new AvgPx(order.Price.getValue())
            );

            if (order.Side.getValue() == Side.BUY)
            {
                _orders[order.Symbol.getValue()].Add(order);
                _exposures[order.Symbol.getValue()] += order.OrderQty.getValue() * order.Price.getValue();
            }
            else if (order.Side.getValue() == Side.SELL)
            {
                var matchingOrders = _orders[order.Symbol.getValue()].Where(o => o.Price.getValue() == order.Price.getValue() && o.OrderQty.getValue() >= order.OrderQty.getValue()).ToList();
                decimal executedQuantity = 0;
                foreach (var matchingOrder in matchingOrders)
                {
                    executedQuantity += matchingOrder.OrderQty.getValue();
                    _orders[order.Symbol.getValue()].Remove(matchingOrder);
                }
                _exposures[order.Symbol.getValue()] -= executedQuantity * order.Price.getValue();
            }

            Session.SendToTarget(execution, _session.SessionID);
            Console.WriteLine(_exposures[order.Symbol.getValue()]);
        }

    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
        Console.WriteLine("To Admin OrderGenerator:  " + message);
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {

        Console.WriteLine("Got message from Admin OrderGenerator:    " + message.ToString());

    }
}