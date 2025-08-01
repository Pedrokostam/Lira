﻿using System;
using System.Management.Automation;
using System.Reflection;
using Lira.Jql;
using Lira.Objects;
using LiraPS.Completers;
using LiraPS.Transformers;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsDiagnostic.Test, "Time")]
    [Alias("tt")]
    public class TestTime : LiraCmdlet
    {
  
        [Parameter]
        [DateTransformer(outputIJqlDate:false, mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompletionAttribute))]
        public DateTimeOffset DateStart { get; set; } = default;
        [Parameter]
        [DateTransformer(outputIJqlDate: false, mode: DateMode.End)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompletionAttribute))]
        public DateTimeOffset DateEnd { get; set; } = default;
        [Parameter]
        [DateTransformer(outputIJqlDate: false, mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompletionAttribute))]
        public DateTimeOffset DateCurrent { get; set; } = default;

        [Parameter]
        [DateTransformer(outputIJqlDate: true, mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompletionAttribute))]
        public IJqlDate JqlStart { get; set; } = default!;
        [Parameter]
        [DateTransformer(outputIJqlDate: true, mode: DateMode.End)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompletionAttribute))]
        public IJqlDate JqlEnd { get; set; } = default!;
        [Parameter]
        [DateTransformer(outputIJqlDate: true, mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompletionAttribute))]
        public IJqlDate JqlCurrent { get; set; } = default!;

        [Parameter]
        [TimespanTransformer]
        public TimeSpan Time { get; set; } = default;

        protected override void BeginProcessing()
        {
            //base.BeginProcessing();
        }
        protected override void ProcessRecord()
        {
            var q = Prompt("Dawaj datę, frajerze", new LiraPS.Completers.JqlDateCurrentArgumentCompletionAttribute());
            WriteObject(q);
            if (TestBoundParameter(nameof(DateCurrent))){ WriteObject(DateCurrent); }
            if (TestBoundParameter(nameof(DateStart))){ WriteObject(DateStart); }
            if (TestBoundParameter(nameof(DateEnd))){ WriteObject(DateEnd); }
            if (TestBoundParameter(nameof(JqlCurrent))){ WriteObject(JqlCurrent); }
            if (TestBoundParameter(nameof(JqlStart))){ WriteObject(JqlStart); }
            if (TestBoundParameter(nameof(JqlEnd))){ WriteObject(JqlEnd); }
            if (TestBoundParameter(nameof(Time))){ WriteObject(Time); }
        }
    }
}
