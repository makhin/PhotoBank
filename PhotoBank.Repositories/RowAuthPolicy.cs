using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBank.Repositories
{
    public class RowAuthPolicy<TTable> : IRowAuthPolicy<TTable>
    {
        public IRowAuthPoliciesContainer Parent { get; private set; }
        public Expression<Func<TTable, bool>> Expression { get; }
        public Type TableType { get; }

        public RowAuthPolicy(Expression<Func<TTable, bool>> expression, IRowAuthPoliciesContainer parent)
        {
            Expression = expression;
            Parent = parent;
            TableType = typeof(TTable);
        }
    }
}
