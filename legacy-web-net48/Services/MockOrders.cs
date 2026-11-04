using System;
using System.Collections.Generic;

namespace LegacyWeb.Services
{
    public class Order
    {
        public int Number { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Fake order data, seeded from the company id so the same company always sees the
    /// same orders instead of a hardcoded static list.
    /// </summary>
    public static class MockOrders
    {
        private static readonly string[] Statuses = { "Pending", "Shipped", "Delivered" };

        public static List<Order> For(string companyId)
        {
            var seed = string.IsNullOrEmpty(companyId) ? 0 : companyId.GetHashCode();
            var random = new Random(seed);
            var orders = new List<Order>();

            for (var i = 0; i < 5; i++)
            {
                orders.Add(new Order
                {
                    Number = 1000 + random.Next(0, 9000),
                    Status = Statuses[random.Next(Statuses.Length)],
                    Total = Math.Round((decimal)(random.NextDouble() * 5000), 2)
                });
            }

            return orders;
        }
    }
}
