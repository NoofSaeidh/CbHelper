using SoapHelp.Default18;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoapHelp
{
    class Program
    {
        static void Main(string[] args)
        {
            var rand = new Random();
            using (var client = new DefaultSoapClient())
            {
                client.Login("admin", "123", null, null, null);
                try
                {
                    var email = client.Put(new Email
                    {
                        Subject = "tmp_e_" + rand.Next(),
                        To = "a@a.a",
                        From = "administrator",
                    });

                    var activity = client.Put(new Activity
                    {
                        Summary = "tmp_a_" + rand.Next(),
                        Type = "C"
                    });

                    var invoice = client.Put(new SalesOrder
                    {
                        CustomerID = "ABARTENDE",
                        Description = "hu",
                        OrderType = "SO"
                    });

                    var res_e = client.Invoke(email, new LinkEntityToEmail
                    {
                        RelatedEntity = "SO," + invoice.OrderNbr.Value,
                        Type = "PX.Objects.SO.SOOrder"
                    });

                    var res_a = client.Invoke(activity, new LinkEntityToActivity
                    {
                        RelatedEntity = "SO," + invoice.OrderNbr.Value,
                        Type = "PX.Objects.SO.SOOrder"
                    });
                }
                finally
                {
                    client.Logout();
                }
            }
        }
    }

    namespace Default18
    {
        public partial class StringValue
        {
            public static implicit operator StringValue(string value) => new StringValue { Value = value };
        }

        public partial class DefaultSoapClient
        {
            public T Put<T>(T entity) where T : Entity
            {
                return (T)Put((Entity)entity);
            }
        }
    }
}
