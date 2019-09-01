﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fop.Exceptions;
using Fop.Filter;
using Fop.Order;
using Fop.Utils;

namespace Fop.FopExpression
{
    public class FopExpressionBuilder<T>
    {
        public static IFopRequest Build(string filter, string order, int pageNumber, int pageSize)
        {
            var request = new FopRequest();
            if (!string.IsNullOrEmpty(filter))
            {
                request.FilterList = FilterExpressionBuilder(filter);
            }

            if (!string.IsNullOrEmpty(order))
            {
                var (orderBy, direction) = OrderExpressionBuilder(order);
                request.OrderBy = orderBy;
                request.Direction = direction;
            }

            if (pageNumber > 0 && pageSize > 0)
            {
                request.PageNumber = pageNumber;
                request.PageSize = pageSize;
            }

            return request;
        }

        private static (string, OrderDirection) OrderExpressionBuilder(string order)
        {
            order = order.ToLower();

            var orderParts = order.Split(';');

            var direction = orderParts[1] == "desc" ? OrderDirection.Desc : OrderDirection.Asc;

            return (orderParts[0], direction);
        }

        private static IEnumerable<IFilterList> FilterExpressionBuilder(string filter)
        {
            filter = filter.ToLower();
            var multipleLogicParts = filter.Split('$');
            var filterList = new IFilterList[multipleLogicParts.Length];

            for (var i = 0; i < multipleLogicParts.Length; i++)
            {
                var multipleLogicPart = multipleLogicParts[i];
                var filterLogicParts = multipleLogicPart.Split(';');

                var logicOperator = filterLogicParts[filterLogicParts.Length - 1];
                if (logicOperator != "and" && logicOperator != "or")
                {
                    throw new LogicOperatorNotFoundException($"You have to pass your logic operator [and, or] but you passed {logicOperator}");
                }

                filterList[i] = new FilterList
                {
                    Filters = new Filter.Filter[filterLogicParts.Length - 1],
                    Logic = filterLogicParts[filterLogicParts.Length - 1] == "or" ? FilterLogic.Or : FilterLogic.And
                };

                for (var j = 0; j < filterLogicParts.Length - 1; j++)
                {
                    var filterLogicPart = filterLogicParts[j];

                    var (key, value) = Operator.Dictionary.FirstOrDefault(x => filterLogicPart.Contains(x.Key));

                    if (key == null)
                    {
                        throw new FilterOperatorNotFoundException(
                            $"{filterLogicPart} is not found in our Operator.Dictionary");
                    }

                    var filterObject = filterLogicPart.Split(key);

                    var property = typeof(T).GetProperties().FirstOrDefault(x => x.Name.ToLower() == filterObject[0]);
                    ((Filter.Filter[])filterList[i].Filters)[j] = new Filter.Filter
                    {
                        Operator = value,
                        DataType = GetFilterDataTypes(property),
                        Key = typeof(T).Name + "." + property.Name,
                        Value = filterObject[1]
                    };
                }
            }

            return filterList;
        }

        #region [ Helpers ]

        private static FilterDataTypes GetFilterDataTypes(PropertyInfo pi)
        {
            if (pi.PropertyType.Name == "Int32")
            {
                return FilterDataTypes.Int;
            }

            if (pi.PropertyType.Name == "String")
            {
                return FilterDataTypes.String;
            }

            if (pi.PropertyType.Name == "Char")
            {
                return FilterDataTypes.Char;
            }

            if (pi.PropertyType.Name == "DateTime")
            {
                return FilterDataTypes.DateTime;
            }

            throw new ArgumentOutOfRangeException();
        }

        #endregion


    }
}