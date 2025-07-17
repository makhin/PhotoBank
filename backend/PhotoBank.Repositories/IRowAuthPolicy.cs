using System;
using System.Linq.Expressions;

namespace PhotoBank.Repositories;

public interface IRowAuthPolicy<TTable>
{
    Expression<Func<TTable, bool>> Expression { get; }
}