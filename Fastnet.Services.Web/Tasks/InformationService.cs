using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Services.Tasks
{
    public class InformationOptions
    {
        public string ApiUrl { get; set; } = @"https://ipapi.co/ip";
        public bool SendInformationEmail { get; set; } = true;
        public string FromAddress { get; set; } = "asimshah2009@gmail.com";
        public string ToAddress { get; set; } = "asimshah@hotmail.com";
    }
    public class InformationService : SinglePipelineTask // ScheduledTask, IPipelineTask
    {
        private MailOptions mailOptions;
        private InformationOptions infoOptions;
        public InformationService(ILoggerFactory loggerFactory, IOptions<InformationOptions> informationOptions, IOptions<MailOptions> mailOptions) : base(loggerFactory)
        {
            this.mailOptions = mailOptions.Value;
            this.infoOptions = informationOptions.Value;
        }
        public override TimeSpan StartAfter => TimeSpan.Zero;
        protected override async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            var externalIp = await GetExternalIp();
            SendInformationEmail(externalIp);
            return null;
        }

        private void SendInformationEmail(string externalIp)
        {
            var body = $"Current external Ip address is {externalIp}";
            var ms = new MailSender(this.mailOptions);
            ms.Send(infoOptions.FromAddress, infoOptions.ToAddress, "Network information", body);
        }

        private async Task<string> GetExternalIp()
        {
            var wc = new WebClient();
            var externalIp = await wc.DownloadStringTaskAsync(infoOptions.ApiUrl);
            log.Information($"current external ip address is {externalIp}");
            var rx = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            return rx.Matches(externalIp)[0].ToString();
        }
    }
}
