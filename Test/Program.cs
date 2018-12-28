using EFCoreExtentions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new TestContext())
            {
                var list = context.User.Select<UserModel>().ToList();

                var user1 = context.User.AsNoTracking().FirstOrDefault(x=>x.Id==2);

                Console.WriteLine($"-----------Before Update --------------------");
                Console.WriteLine($"{user1.Id}:{user1.Name}:{user1.RoleId}");

                context.User.Where(x => x.Id == 2).RestValue(x => x.Name == (x.Name + " Add Bob") && x.RoleId == (x.RoleId + 1));

                var user2 = context.User.AsNoTracking().FirstOrDefault(x => x.Id == 2);

                Console.WriteLine($"-----------After Update --------------------");
                Console.WriteLine($"{user2.Id}:{user2.Name}:{user2.RoleId}");
            }
            Console.WriteLine($"------------结束--------------------");
            Console.ReadLine();
        }
    }
}
