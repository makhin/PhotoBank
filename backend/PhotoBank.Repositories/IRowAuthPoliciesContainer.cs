using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.Repositories
{
    public interface IRowAuthPoliciesContainer
    {
        IEnumerable<IRowAuthPolicy<TTable>> GetPolicies<TTable>();
        IRowAuthPoliciesContainer Register<TTable>(Expression<Func<TTable, bool>> expression);
    }
}
