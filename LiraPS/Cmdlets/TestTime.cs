using System;
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
    public sealed class TestTime : LiraCmdlet
    {

        [Parameter]
        [DateTimeOffsetDateTransformerAttribute(mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompleter))]
        public DateTimeOffset DateStart { get; set; } = default;
        [Parameter]
        [DateTimeOffsetDateTransformerAttribute(mode: DateMode.End)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompleter))]
        public DateTimeOffset DateEnd { get; set; } = default;
        [Parameter]
        [DateTimeOffsetDateTransformerAttribute(mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompleter))]
        public DateTimeOffset DateCurrent { get; set; } = default;

        [Parameter]
        [JqlDateTransformerAttribute(mode: DateMode.Start)]
        [ArgumentCompleter(typeof(JqlDateStartArgumentCompleter))]
        public IJqlDate JqlStart { get; set; } = default!;
        [Parameter]
        [JqlDateTransformerAttribute(mode: DateMode.End)]
        [ArgumentCompleter(typeof(JqlDateEndArgumentCompleter))]
        public IJqlDate JqlEnd { get; set; } = default!;
        [Parameter]
        [JqlDateTransformerAttribute(mode: DateMode.Current)]
        [ArgumentCompleter(typeof(JqlDateCurrentArgumentCompleter))]
        public IJqlDate JqlCurrent { get; set; } = default!;

        [Parameter]
        [TimespanTransformer]
        public TimeSpan Time { get; set; } = default;

        protected override void BeginProcessing()
        {
            Console.CancelKeyPress += DumpLogEvent;

            //base.BeginProcessing();
        }
        protected override void ProcessRecord()
        {
            var q = Prompt("Dawaj datę, frajerze", new LiraPS.Completers.JqlDateCurrentArgumentCompleter());
            WriteObject(q);
            if (TestBoundParameter(nameof(DateCurrent)))
            { WriteObject(DateCurrent); }
            if (TestBoundParameter(nameof(DateStart)))
            { WriteObject(DateStart); }
            if (TestBoundParameter(nameof(DateEnd)))
            { WriteObject(DateEnd); }
            if (TestBoundParameter(nameof(JqlCurrent)))
            { WriteObject(JqlCurrent); }
            if (TestBoundParameter(nameof(JqlStart)))
            { WriteObject(JqlStart); }
            if (TestBoundParameter(nameof(JqlEnd)))
            { WriteObject(JqlEnd); }
            if (TestBoundParameter(nameof(Time)))
            { WriteObject(Time); }
        }
    }
}
