using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using PhotoBank.DbContext.Models;

namespace PhotoBank.Repositories
{
    internal class RowAuthPoliciesContainer : IRowAuthPoliciesContainer
    {
        private readonly List<Tuple<Type, object>> _policies = new();
        private const string ImpersonatedPrincipalKey = "ImpersonatedPrincipal";

        public IRowAuthPoliciesContainer Register<TTable>(Expression<Func<TTable, bool>> expression)
        {
            var policy = new RowAuthPolicy<TTable>(expression, this);
            _policies.Add(new Tuple<Type, object>(policy.TableType, policy));
            return policy.Parent;
        }

        public IEnumerable<IRowAuthPolicy<TTable>> GetPolicies<TTable>()
        {
            return _policies.Where(p => p.Item1 == typeof(TTable)).Select(p => (IRowAuthPolicy<TTable>) p.Item2);
        }

        public static IRowAuthPoliciesContainer ConfigureRowAuthPolicies(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var user = httpContext.Items.ContainsKey(ImpersonatedPrincipalKey) &&
                       httpContext.Items[ImpersonatedPrincipalKey] is ClaimsPrincipal impersonated
                ? impersonated
                : httpContext.User;
            var rowAuthPoliciesContainer = new RowAuthPoliciesContainer();
            
            if (!user.HasClaim(c => c.Type == "AllowAdultContent" && c.Value == "True"))
            {
                rowAuthPoliciesContainer.Register<Photo>(p => !p.IsAdultContent);
            }

            if (!user.HasClaim(c => c.Type == "AllowRacyContent" && c.Value == "True"))
            {
                rowAuthPoliciesContainer.Register<Photo>(p => !p.IsRacyContent);
            }

            if (user.HasClaim(c => c.Type == "AllowStorage"))
            {
                var storages = user.Claims.Where(c => c.Type == "AllowStorage").Select(c => int.Parse(c.Value)).ToList();
                rowAuthPoliciesContainer.Register<Photo>(p => storages.Contains(p.StorageId));
                rowAuthPoliciesContainer.Register<Storage>(s => storages.Contains(s.Id));
            }

            if (user.HasClaim(c => c.Type == "TakenBefore"))
            {
                var date = DateTime.Parse(user.Claims.First(c => c.Type == "TakenBefore").Value);
                rowAuthPoliciesContainer.Register<Photo>(p => !p.TakenDate.HasValue || (p.TakenDate.HasValue && p.TakenDate < date));
            }

            if (user.HasClaim(c => c.Type == "AllowPersonGroup"))
            {
                var groupIds = user.Claims.Where(c => c.Type == "AllowPersonGroup").Select(c => int.Parse(c.Value)).ToList();
                rowAuthPoliciesContainer.Register<Person>(p =>
                    p.PersonGroups.Any(pg => groupIds.Contains(pg.Id)));
            }

            return rowAuthPoliciesContainer;
        }
    }
}
