using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TFSHelper
{
    public class TFSManager
    {
        public Configuration _configuration;

        public Operation? Mode { get { return _configuration.Mode; } }

        public TFSManager(string[] args)
        {
            _configuration = Args.Configuration.Configure<Configuration>().CreateAndBind(args);
        }

        public void WriteHelp()
        {
            Console.WriteLine(@"Usage: TFSHelper /source <TfsCollectionUri> /projectsource <SourceProjectName> /mode list");
            Console.WriteLine("e.g. TFSHelper /source http://tfs.24orecww.com:8080/tfs/INTRANET /projectsource SharePoint2013 /mode list");

            Exit();
        }



        public void List()
        {
            if (string.IsNullOrEmpty(_configuration.Source) ||
                string.IsNullOrEmpty(_configuration.ProjectSource))
            {
                WriteHelp();
                return;
            }



            IBuildServer buildServer = GetTfsBuildServer(_configuration.Source);

            IBuildDefinition[] sourceBuildDefinitions = buildServer.QueryBuildDefinitions(_configuration.ProjectSource);
            if (sourceBuildDefinitions.Count() == 0)
            {
                Console.WriteLine("No build to display");
            }
            else
            {

                Console.WriteLine(string.Join(System.Environment.NewLine, sourceBuildDefinitions
                    .Select(x => string.Format("Build Name: {0}; Build Description: {1}", x.Name, x.Description))
                    .ToArray()));
            }

            VersionControlServer versionServer = GetTfsVersionControlServer(_configuration.Source);
            TeamProject tProject = versionServer.GetTeamProject(_configuration.ProjectSource);
            if (tProject == null)
            {
                Console.WriteLine("Version server not valid");
                Exit();
                return;
            }

            ItemSet items = versionServer.GetItems(tProject.ServerItem, RecursionType.OneLevel);
            if (items.Items.Count() == 0)
            {
                Console.WriteLine("No items to display");
                Exit();
                return;
            }

            Console.WriteLine(string.Join(System.Environment.NewLine,
                items.Items
                .Select(x => string.Format("Name: {0}", x.ServerItem.Replace(tProject.ServerItem + "/", string.Empty)))
                .ToArray()));

            Exit();
        }

        public void BuildCopy()
        {
            if (string.IsNullOrEmpty(_configuration.Source) ||
                string.IsNullOrEmpty(_configuration.ProjectSource) ||
                string.IsNullOrEmpty(_configuration.Destination) ||
                string.IsNullOrEmpty(_configuration.ProjectDestination))
            {
                WriteHelp();
                return;
            }

            IBuildServer buildServerSource = GetTfsBuildServer(_configuration.Source);
            IBuildServer buildServerDestination = GetTfsBuildServer(_configuration.Destination);

            IBuildDefinition[] sourceBuildDefinitions = buildServerSource.QueryBuildDefinitions(_configuration.ProjectSource);

            foreach (var sourceBuildDef in sourceBuildDefinitions)
            {
                IBuildDefinition targetBuildDef = buildServerDestination.CreateBuildDefinition(_configuration.ProjectDestination);
                Copy(sourceBuildDef, targetBuildDef);
                targetBuildDef.Save();
            }

        }


        public static void GetAllCollections(string tfsCollectionUri)
        {
            Uri configurationServerUri = new Uri(tfsCollectionUri);
            TfsConfigurationServer configurationServer = TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);

            ITeamProjectCollectionService tpcService = configurationServer.GetService<ITeamProjectCollectionService>();

            var tpc = tpcService.GetCollections();


        }

        private IBuildServer GetTfsBuildServer(string tfsCollectionUri)
        {

            /*NetworkCredential netCred = new NetworkCredential("**", "**", "**");
            BasicAuthCredential basicCred = new BasicAuthCredential(netCred);
            TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
            tfsCred.AllowInteractive = false;
            */



            var collection = new TfsTeamProjectCollection(new Uri(tfsCollectionUri));

            //collection.Authenticate();


            collection.EnsureAuthenticated();
            return collection.GetService<IBuildServer>();
        }

        private VersionControlServer GetTfsVersionControlServer(string tfsCollectionUri)
        {

            /*NetworkCredential netCred = new NetworkCredential("**", "**", "**");
            BasicAuthCredential basicCred = new BasicAuthCredential(netCred);
            TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
            tfsCred.AllowInteractive = false;
            */


            var collection = new TfsTeamProjectCollection(new Uri(tfsCollectionUri));

            //collection.Authenticate();


            collection.EnsureAuthenticated();
            return collection.GetService<VersionControlServer>();
        }

        private void Copy(IBuildDefinition source, IBuildDefinition target)
        {
            //target.BatchSize = source.BatchSize;
            target.BuildController = source.BuildController;
            target.ContinuousIntegrationType = source.ContinuousIntegrationType;
            target.ContinuousIntegrationQuietPeriod = source.ContinuousIntegrationQuietPeriod;
            target.DefaultDropLocation = source.DefaultDropLocation;
            target.Description = source.Description;
            target.Process = source.Process;
            target.ProcessParameters = source.ProcessParameters;
            //target.QueueStatus = source.QueueStatus;
            //target.TriggerType = source.TriggerType;

            CopySchedules(source, target);
            CopyMappings(source, target);
            CopyRetentionPolicies(source, target);
        }

        private void CopyRetentionPolicies(IBuildDefinition source, IBuildDefinition target)
        {
            target.RetentionPolicyList.Clear();

            foreach (var policy in source.RetentionPolicyList)
            {
                target.AddRetentionPolicy(
                    policy.BuildReason,
                    policy.BuildStatus,
                    policy.NumberToKeep,
                    policy.DeleteOptions
                    );
            }
        }

        private void CopyMappings(IBuildDefinition source, IBuildDefinition target)
        {
            foreach (var mapping in source.Workspace.Mappings)
            {
                target.Workspace.AddMapping(
                    mapping.ServerItem,
                    mapping.LocalItem,
                    mapping.MappingType,
                    mapping.Depth
                    );
            }
        }

        private void CopySchedules(IBuildDefinition source, IBuildDefinition target)
        {
            foreach (var schedule in source.Schedules)
            {
                var newSchedule = target.AddSchedule();
                newSchedule.DaysToBuild = schedule.DaysToBuild;
                newSchedule.StartTime = schedule.StartTime;
                newSchedule.TimeZone = schedule.TimeZone;
            }
        }


        private void Exit()
        {
            Console.WriteLine(System.Environment.NewLine + "Press any key to exit");
            Console.ReadLine();
        }

    }
}
