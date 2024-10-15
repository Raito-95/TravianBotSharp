using Humanizer;
using MainCore.Commands.Abstract;
using MainCore.Common.Models;
using MainCore.UI.Models.Output;
using System.Text.Json;

namespace MainCore.Commands.Queries
{
    [RegisterSingleton<GetJobs>]
    public class GetJobs(IDbContextFactory<AppDbContext> contextFactory) : QueryBase(contextFactory)
    {
        public List<JobDto> Dtos(VillageId villageId)
        {
            using var context = _contextFactory.CreateDbContext();

            var dtos = context.Jobs
                .Where(x => x.VillageId == villageId.Value)
                .OrderBy(x => x.Position)
                .ToDto()
                .ToList();
            return dtos;
        }

        public List<ListBoxItem> Items(VillageId villageId)
        {
            using var context = _contextFactory.CreateDbContext();

            var items = context.Jobs
                .Where(x => x.VillageId == villageId.Value)
                .OrderBy(x => x.Position)
                .ToDto()
                .AsEnumerable()
                .Select(x => new ListBoxItem()
                {
                    Id = x.Id.Value,
                    Content = GetContent(x),
                })
                .ToList();

            return items;
        }

        private static string GetContent(JobDto job)
        {
            switch (job.Type)
            {
                case JobTypeEnums.NormalBuild:
                    {
                        var plan = JsonSerializer.Deserialize<NormalBuildPlan>(job.Content);
                        var twType = (BuildingEnumsTW)plan.Type;
                        return $"在位置 {plan.Location} 建造 {twType.Humanize()} 到等級 {plan.Level}";
                    }
                case JobTypeEnums.ResourceBuild:
                    {
                        var plan = JsonSerializer.Deserialize<ResourceBuildPlan>(job.Content);
                        return $"將 {plan.Plan.Humanize()} 升級到等級 {plan.Level}";
                    }
                default:
                    return job.Content;
            }
        }
    }
}