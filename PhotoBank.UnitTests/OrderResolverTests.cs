using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto.Load;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests
{
    public abstract class EnricherTestBase : IEnricher
    {
        public abstract Type[] Dependencies { get; }
        public bool IsActive => true;
        public async Task Enrich(Photo photo, SourceDataDto path)
        {
            await Task.Run(() =>
            {
                Task.Delay(new Random().Next(500, 1500));
                Debug.WriteLine(this.GetType().Name);
            });
        }
    }

    public class EnAd : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnAn : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    public class EnCap : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCat : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCo : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnFa : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnMe : EnricherTestBase
    {
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class EnOb : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnPr : EnricherTestBase
    {
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class EnTa : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnTh : EnricherTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    [TestFixture]
    public class OrderResolverTests
    {
        [Test]
        public void Test()
        {
            IEnumerable<IEnricher> collection = new EnricherTestBase[]{ new EnAd(), new EnAn(), new EnCap(), new EnCat(), new EnCo(), new EnFa(), new EnMe(), new EnOb(), new EnPr(), new EnTa(), new EnTh() };

            IEnumerable<IEnricher> enrichers = collection.Where(e => e.Dependencies.Length == 0).ToList();

            Dictionary<Type, Task> tasks0level = enrichers.ToDictionary(enricher => enricher.GetType(), enricher => enricher.Enrich(null, null));

            tasks0level.Select(e => e.Value);
        }
    }
}
