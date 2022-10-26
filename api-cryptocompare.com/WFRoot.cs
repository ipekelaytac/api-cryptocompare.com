using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace api_cryptocompare.com
{
    public partial class WFRoot : Form
    {
        #region Const Methods
        public WFRoot() => InitializeComponent();
        private void WFRoot_Shown(object sender, EventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            task = Task.Factory.StartNew(Run, cancellationToken);
        }
        private void WFRoot_FormClosing(object sender, FormClosingEventArgs e) => cancellationTokenSource.Cancel();
        #endregion
        #region Fields And Properties
        private Task task;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private RestRequest request;
        private RestResponse response;
        private RestClient client;
        private ExchangeCollection exchanges;
        #endregion
        #region MainMethods
        private void Run()
        {
            llog("runned");
            llog("pull data in api");
            if (Exec("https://min-api.cryptocompare.com/data/v4/all/exchanges"))
            {
                exchanges = new ExchangeCollection();
                JObject jobj = JsonConvert.DeserializeObject<JObject>(response.Content);
                llog($@"Api Response Message : {jobj["Response"].Value<string>()}");
                if (jobj["Type"].Value<string>() is "100")
                {
                    llog("start to splits data");
                    jobj["Data"]["exchanges"].ToList().ForEach((jtokExchange) =>
                    {
                        var exchangeProp = jtokExchange as JProperty;
                        Exchange exchange = new Exchange()
                        {
                            Name = exchangeProp.Name
                            ,
                            HasFroms = jtokExchange.First["pairs"].FirstOrDefault() is null ? false : true
                        };
                        jtokExchange.First["pairs"].ToList().ForEach((jtokFrom) =>
                        {
                            var fromProp = jtokFrom as JProperty;
                            From from = new From(exchange)
                            {
                                Name = fromProp.Name
                                ,
                                HasTos = jtokFrom.First["tsyms"].FirstOrDefault() is null ? false : true
                            };
                            jtokFrom.First["tsyms"].ToList().ForEach((jtokTo) =>
                            {
                                var toProp = jtokTo as JProperty;
                                To to = new To(from)
                                {
                                    Name = toProp.Name
                                    ,
                                    HasValue = jtokTo.First.HasValues
                                };
                                if (to.HasValue)
                                {
                                    to.Start_ts = jtokTo.First["histo_minute_start_ts"].Value<string>();
                                    to.End_ts = jtokTo.First["histo_minute_end_ts"].Value<string>();
                                    to.StartDateString = jtokTo.First["histo_minute_start"].Value<string>();
                                    to.EndDateString = jtokTo.First["histo_minute_end"].Value<string>();
                                }
                                from.Tos.Add(to);
                                exchanges.AllTo.Add(to);
                            });
                            exchange.Froms.Add(from);
                        });
                        exchanges.Add(exchange);
                    });
                }
                else
                {
                    llog("warn data");
                }
            }
            else
            {
                llog("Error! Request timeout or Response status code eq 0");
            }
            llog("end");
            Control();
        }
        private void Control()
        {
            if (exchanges is null || exchanges.Count == 0)
                return;
            llog("Control is started");
            // // // //
            // //
            // //
            //      Burada ciftler kontrol edilecek
            // //
            // //
            // //
            llog("Control is finished");
        }
        #endregion
        #region Supporter Methods
#pragma warning disable
        bool Exec(string uri)
        {
            client = new RestClient()
            {
                Timeout = 30 * 1000
                ,
                ReadWriteTimeout = 30 * 1000
                ,
                Encoding = Encoding.GetEncoding(1254)
            };
            request = new RestRequest(uri, Method.GET)
            {
                Timeout = 30 * 1000
                ,
                ReadWriteTimeout = 30 * 1000
            };
            response = client.Execute(request) as RestResponse;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return true;
            else
                return false;
        }
        void llog(string @string) => IAction(() =>
                                   {
                                       TbxLog.Text += $@"{@string}{Environment.NewLine}";
                                       TbxLog.SelectionStart = TbxLog.TextLength;
                                       TbxLog.ScrollToCaret();
                                   });
        void waitForMilliSeconds(int milliSeconds) => Task.Factory.StartNew(() => { Thread.Sleep(milliSeconds); }).Wait();
        void IAction(Action action)
        {
            if (InvokeRequired)
                Invoke(action);
            else
                action.Invoke();
        }
#pragma warning restore
        #endregion
        #region Classes
        public class ExchangeCollection : List<Exchange>
        {
            public List<To> AllTo { get; set; }
            public ExchangeCollection() => AllTo = new List<To>();
        }
        public class Exchange
        {
            public string Name { get; set; }
            public bool HasFroms { get; set; }
            public List<From> Froms { get; set; }
            public Exchange()
            {
                Name = string.Empty;
                Froms = new List<From>();
            }
            public override string ToString()
            {
                return $@"{Name}    {HasFroms}";
            }
        }
        public class From
        {
            public Exchange Exchange { get; set; }
            public string Name { get; set; }
            public bool HasTos { get; set; }
            public List<To> Tos { get; set; }
            public From(Exchange exchange)
            {
                Exchange = exchange;
                Name = string.Empty;
                Tos = new List<To>();
            }
            public override string ToString()
            {
                return $@"{Name}    {HasTos}";
            }
        }
        public class To
        {
            public From From { get; set; }
            public string Name { get; set; }
            public bool HasValue { get; set; }
            public string Start_ts { get; set; }
            public string End_ts { get; set; }
            public string StartDateString { get; set; }
            public string EndDateString { get; set; }
            public string FullName => $@"{From.Name}{Name}";
            public DateTime StartDateTime => DateTime.Parse(StartDateString);
            public DateTime EndDateTime => DateTime.Parse(EndDateString);
            public To(From from)
            {
                From = from;
            }
            public override string ToString()
            {
                return $@"{Name}    {HasValue}";
            }
        }
        #endregion
    }
}