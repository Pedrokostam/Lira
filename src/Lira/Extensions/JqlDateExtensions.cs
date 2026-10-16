using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lira.Jql;

namespace Lira.Extensions;

public static class JqlDateExtensions
{
    public static BoundedJqlDate ToBounded(this IJqlDate jqlDate, JqlDateBoundary boundary = JqlDateBoundary.Inclusive)
    {
        return new BoundedJqlDate(jqlDate, boundary);
    }
}
