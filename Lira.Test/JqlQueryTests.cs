using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using Lira.Jql;

namespace Lira.Test;

public class JqlQueryTests
{
    [Fact(DisplayName ="AllFields does reference all IJqlQueryItem members")]
    public void AllFieldsDoesContainAllFields()
    {
        var query = new Lira.Jql.JqlQuery();
        var allFields = (IEnumerable<IJqlQueryItem>)query.GetType().GetProperty("AllFields", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(query)!;
        var t = typeof(JqlQuery).GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
        var tt = t.Select(x=>x.PropertyType).ToList();
        var ttt = t.Select(x=>x.PropertyType.IsAssignableTo(typeof(IJqlQueryItem))).ToList();
        var instanceProperies = typeof(JqlQuery).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.PropertyType.IsAssignableTo(typeof(IJqlQueryItem)))
            .Select(x=>(IJqlQueryItem)x.GetValue(query)!).ToList();
        foreach (var named in allFields)
        {
            Assert.Contains(named,instanceProperies);
        }
        foreach (var reflected in instanceProperies)
        {
            Assert.Contains(reflected, allFields);
        }
    }
}