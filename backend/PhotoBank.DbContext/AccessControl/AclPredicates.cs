using System;
using System.Linq;
using System.Linq.Expressions;
using PhotoBank.DbContext.Models;

namespace PhotoBank.AccessControl;

public static class AclPredicates
{
    public static Expression<Func<Photo, bool>> PhotoWhere(Acl acl)
    {
        if (acl.StorageIds.Length == 0)
        {
            return _ => false;
        }

        var basePredicate = CreateBasePhotoPredicate(acl);
        var datePredicate = BuildDatePredicate(acl.DateRanges);

        return basePredicate.AndAlso(datePredicate);
    }

    private static Expression<Func<Photo, bool>> CreateBasePhotoPredicate(Acl acl)
    {
        var storage = acl.StorageIds;
        var groups = acl.AllowedPersonGroupIds;
        var nsfw = acl.CanSeeNsfw;

        return p =>
            p.Files.Any(f => storage.Contains(f.StorageId)) &&
            (nsfw || (!p.IsAdultContent && !p.IsRacyContent)) &&
            (
                groups.Length == 0
                    ? !p.Faces.Any()
                    : (!p.Faces.Any() || p.Faces.Any(f => f.PersonId != null &&
                                                          f.Person.PersonGroups.Any(pg => groups.Contains(pg.Id))))
            );
    }

    private static Expression<Func<Photo, bool>> BuildDatePredicate(AclDateRange[] ranges)
    {
        if (ranges.Length == 0)
        {
            return _ => true;
        }

        var predicate = (Expression<Func<Photo, bool>>)(p => !p.TakenDate.HasValue);

        for (var i = 0; i < ranges.Length; i++)
        {
            predicate = predicate.OrElse(BuildRangePredicate(ranges[i]));
        }

        return predicate;
    }

    private static Expression<Func<Photo, bool>> BuildRangePredicate(AclDateRange range)
        => p =>
            p.TakenDate.HasValue &&
            p.TakenDate.Value >= range.From &&
            p.TakenDate.Value <= range.To;

    public static Expression<Func<Person, bool>> PersonWhere(Acl acl)
        => p => acl.AllowedPersonGroupIds.Length != 0 && p.PersonGroups.Any(pg => acl.AllowedPersonGroupIds.Contains(pg.Id));

    public static Expression<Func<Storage, bool>> StorageWhere(Acl acl)
        => s => acl.StorageIds.Length != 0 && acl.StorageIds.Contains(s.Id);

    private static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.AndAlso);

    private static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.OrElse);

    private static Expression<Func<T, bool>> Combine<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, Func<Expression, Expression, BinaryExpression> merge)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body) ?? throw new InvalidOperationException("Unable to map left expression parameter.");
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body) ?? throw new InvalidOperationException("Unable to map right expression parameter.");
        return Expression.Lambda<Func<T, bool>>(merge(leftBody, rightBody), parameter);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _source ? _target : base.VisitParameter(node);
    }
}
