using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TFSHelper
{

    public enum Operation
    {
        BUILDDUMP,
        LIST,
        BUILDCOPY,
        NONE
    }

    public class CommandObject
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string ProjectSource { get; set; }
        public string ProjectDestination { get; set; }
        public Operation? Mode { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {

            var command = Args.Configuration.Configure<CommandObject>().CreateAndBind(args);

            string tfsSourceServer = string.Empty,
                 tfsDestinationServer = string.Empty,
                sourceProjectName = string.Empty,
                destinationProjectName = string.Empty;

            if (command.Mode != Operation.LIST)
            {
                Console.WriteLine(@"Usage: TFSHelper /source <TfsCollectionUri> /projectsource <SourceProjectName> /mode list
e.g. TFSHelper /source http://tfs.24orecww.com:8080/tfs/INTRANET /projectsource SharePoint2013 /mode list");
                Exit();
            }
            else
            {

                tfsSourceServer = command.Source;
                sourceProjectName = command.ProjectSource;

                tfsDestinationServer = command.Destination;
                destinationProjectName = command.ProjectDestination;

                IBuildServer buildServer = GetTfsBuildServer(tfsSourceServer);

                //IBuildServer serverNew = GetTfsBuildServer(tfsDestinationServer);

                IBuildDefinition[] sourceBuildDefinitions = buildServer.QueryBuildDefinitions(sourceProjectName);
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

                VersionControlServer versionServer = GetTfsVersionControlServer(tfsSourceServer);
                TeamProject tProject = versionServer.GetTeamProject(sourceProjectName);
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

                /*
                foreach (var sourceBuildDef in sourceBuildDefinitions)
                {
                    IBuildDefinition targetBuildDef = serverNew.CreateBuildDefinition(destinationProjectName);
                    Copy(sourceBuildDef, targetBuildDef);
                    targetBuildDef.Save();
                }
                 * */
            }
        }


        private static void Exit() {
            Console.WriteLine(System.Environment.NewLine + "Press any key to exit");
            Console.ReadLine();
        }

        static IBuildServer GetTfsBuildServer(string tfsCollectionUri)
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

        static VersionControlServer GetTfsVersionControlServer(string tfsCollectionUri)
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

        static void Copy(IBuildDefinition source, IBuildDefinition target)
        {
            target.BatchSize = source.BatchSize;
            target.BuildController = source.BuildController;
            target.ContinuousIntegrationType = source.ContinuousIntegrationType;
            target.ContinuousIntegrationQuietPeriod = source.ContinuousIntegrationQuietPeriod;
            target.DefaultDropLocation = source.DefaultDropLocation;
            target.Description = source.Description;
            target.Process = source.Process;
            target.ProcessParameters = source.ProcessParameters;
            target.QueueStatus = source.QueueStatus;
            target.TriggerType = source.TriggerType;

            CopySchedules(source, target);
            CopyMappings(source, target);
            CopyRetentionPolicies(source, target);
        }

        private static void CopyRetentionPolicies(IBuildDefinition source, IBuildDefinition target)
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

        private static void CopyMappings(IBuildDefinition source, IBuildDefinition target)
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

        private static void CopySchedules(IBuildDefinition source, IBuildDefinition target)
        {
            foreach (var schedule in source.Schedules)
            {
                var newSchedule = target.AddSchedule();
                newSchedule.DaysToBuild = schedule.DaysToBuild;
                newSchedule.StartTime = schedule.StartTime;
                newSchedule.TimeZone = schedule.TimeZone;
            }
        }
    }
}
