﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;

namespace DiagnosticExplorer.Log4Net;

public class SmtpAppender : AppenderSkeleton
{

    internal const string DefaultHostName = "Default Smtp Host";

    public SmtpAppender()
    {
        Authentication = log4net.Appender.SmtpAppender.SmtpAuthentication.None;
        Priority = MailPriority.Normal;
    }

    public string To { get; set; }

    public string From { get; set; }

    public ILayout Subject { get; set; }

    public string SmtpHost { get; set; }

    public log4net.Appender.SmtpAppender.SmtpAuthentication Authentication { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public MailPriority Priority { get; set; }

    /// <summary>Used to specify the amount of minutes timeout to wait for before resetting that an error occurred on an appender.</summary>
    [Property]
    public TimeSpan FailTimeout { get; set; } = TimeSpan.FromSeconds(-1);

    [RateProperty(ExposeRate = false, ExposeTotal = true)]
    public RateCounter EventsIn { get; set; } = new RateCounter(3);

    [RateProperty(ExposeRate = false, ExposeTotal = true)]
    public RateCounter EventsOut { get; set; } = new RateCounter(3);

    [RateProperty(ExposeRate = false, ExposeTotal = true)]
    public RateCounter EventsErrored { get; set; } = new RateCounter(3);

    protected override bool RequiresLayout
    {
        get { return true; }
    }

    [CollectionProperty(CollectionMode.Categories, CategoryProperty = nameof(SmtpAppenderProxy.SmtpHost))]
    public List<SmtpAppenderProxy> Proxies { get; private set; }

    public override void ActivateOptions()
    {
        base.ActivateOptions();

        string[] hosts = (SmtpHost ?? "").Split(',', ';').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        if (FailTimeout < TimeSpan.Zero)
            FailTimeout = hosts.Length == 1 ? TimeSpan.Zero : TimeSpan.FromMinutes(5);

        Proxies = new List<SmtpAppenderProxy>();

        foreach (string host in hosts)
        {
            if (string.IsNullOrWhiteSpace(host) || string.Equals("Default", host.Trim(), StringComparison.InvariantCultureIgnoreCase))
                Proxies.Add(new SmtpAppenderProxy(this, DefaultHostName, FailTimeout));
            else
                Proxies.Add(new SmtpAppenderProxy(this, host, FailTimeout));
        }

        if (!Proxies.Any())
            Proxies.Add(new SmtpAppenderProxy(this, DefaultHostName, FailTimeout));

        DiagnosticManager.Register(this, Name, "Log4Net");
    }

    protected override void Append(LoggingEvent loggingEvent)
    {
        PerformSend(loggingEvent);
    }

    protected void PerformSend(LoggingEvent loggingEvent)
    {
        StringWriter bodyWriter = new StringWriter();

        if (Layout.Header != null)
            bodyWriter.Write(Layout.Header);

        // Render the event and append the text to the buffer
        RenderLoggingEvent(bodyWriter, loggingEvent);

        if (Layout.Footer != null)
            bodyWriter.Write(Layout.Footer);


        MailMessage message = new MailMessage();
        message.Body = bodyWriter.ToString();
        message.From = new MailAddress(From);
        message.To.Add(To);
        message.Subject = RenderSubject(loggingEvent);
        message.Priority = Priority;

        SendToProxies(message);
    }

    private string RenderSubject(LoggingEvent loggingEvent)
    {
        if (Subject == null)
            return "No Subject";

        try
        {
            StringWriter subjectWriter = new StringWriter();
            //format the layout
            Subject.Format(subjectWriter, loggingEvent);

            return subjectWriter.ToString();
        }
        catch (Exception ex)
        {
            return $"Bad subject format: {ex.Message}";
        }
    }

    protected void SendToProxies(MailMessage message)
    {
        foreach (SmtpAppenderProxy proxy in Proxies)
        {
            if (proxy.TrySend(message))
            {
                EventsOut.Register(1);
                break;
            }
            EventsErrored.Register(1);
            RecordAppenderError(proxy);
        }
    }

    protected override void OnClose()
    {
        base.OnClose();
        DiagnosticManager.Unregister(this);
    }

    private void RecordAppenderError(SmtpAppenderProxy appender)
    {
        ForwardingAppenderBase.LogLogError(GetType(), $"appender [{appender.SmtpHost}] has an error.");
    }

}