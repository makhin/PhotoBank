using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PhotoBank.DbContext.Models;
using PhotoBank.Dto;
using PhotoBank.Dto.Load;
using PhotoBank.Services;
using PhotoBank.Services.Enrichers;

namespace PhotoBank.UnitTests
{
    public abstract class EnricheTestBase : IEnricher
    {
        public abstract Type[] Dependencies { get; }
        public bool IsActive => true;
        public async Task Enrich(Photo photo, SourceDataDto path)
        {
            await Task.Run(() =>
            {
                Task.Delay(1000);
                Debug.WriteLine(this.GetType().Name);
            });
        }
    }

    public class EnAd : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnAn : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    public class EnCap : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCat : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnCo : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnFa : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnMe : EnricheTestBase
    {
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class EnOb : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnPr : EnricheTestBase
    {
        public override Type[] Dependencies => Array.Empty<Type>();
    }

    public class EnTa : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnAn) };
    }

    public class EnTh : EnricheTestBase
    {
        public override Type[] Dependencies => new[] { typeof(EnPr) };
    }

    [TestFixture]
    public class OrderResolverTests
    {
        [Test]
        public void Test()
        {
            IEnumerable<IEnricher> collection = new EnricheTestBase[]{ new EnAd(), new EnAn(), new EnCap(), new EnCat(), new EnCo(), new EnFa(), new EnMe(), new EnOb(), new EnPr(), new EnTa(), new EnTh() };

            IEnumerable<IEnricher> enrichers = collection.Where(e => e.Dependencies.Length == 0).ToList();

            Dictionary<Type, Task> tasks0level = enrichers.ToDictionary(enricher => enricher.GetType(), enricher => enricher.Enrich(null, null));

            tasks0level.Select(e => e.Value);

        }
    }
}
