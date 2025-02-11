﻿using MainCore.Common.Models;
using System.Text.Json;

namespace MainCore.Commands.UI.Build
{
    public class ImportCommand
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IDialogService _dialogService;
        private readonly IMediator _mediator;

        public ImportCommand(IDbContextFactory<AppDbContext> contextFactory = null, IDialogService dialogService = null, IMediator mediator = null)
        {
            _contextFactory = contextFactory ?? Locator.Current.GetService<IDbContextFactory<AppDbContext>>();
            _dialogService = dialogService ?? Locator.Current.GetService<IDialogService>();
            _mediator = mediator ?? Locator.Current.GetService<IMediator>();
        }

        private static readonly List<int> _excludedLocations = new() { 26, 39, 40 }; //main building, rallypoint and wall

        private static readonly Dictionary<ResourcePlanEnums, List<BuildingEnums>> _fieldList = new()
        {
            {ResourcePlanEnums.AllResources, new(){
                BuildingEnums.Woodcutter,
                BuildingEnums.ClayPit,
                BuildingEnums.IronMine,
                BuildingEnums.Cropland,}},
            {ResourcePlanEnums.ExcludeCrop, new() {
                BuildingEnums.Woodcutter,
                BuildingEnums.ClayPit,
                BuildingEnums.IronMine,}},
            {ResourcePlanEnums.OnlyCrop, new() {
                BuildingEnums.Cropland,}},
        };

        public async Task Execute(AccountId accountId, VillageId villageId)
        {
            var path = _dialogService.OpenFileDialog();
            List<JobDto> jobs;
            try
            {
                var jsonString = await File.ReadAllTextAsync(path);
                jobs = JsonSerializer.Deserialize<List<JobDto>>(jsonString);
            }
            catch
            {
                _dialogService.ShowMessageBox("警告", "無效的檔案。");
                return;
            }

            var confirm = _dialogService.ShowConfirmBox("警告", "TBS將刪除資源田建築任務，如果其位置與當前村莊不匹配。");
            if (!confirm) return;

            var modifiedJobs = GetModifiedJobs(villageId, jobs);
            AddJobToDatabase(villageId, modifiedJobs);

            await _mediator.Publish(new JobUpdated(accountId, villageId));

            _dialogService.ShowMessageBox("資訊", "任務已匯入");
        }

        private static IEnumerable<JobDto> GetModifiedJobs(VillageId villageId, List<JobDto> jobs)
        {
            var buildings = new GetBuildings().Execute(villageId);

            var changedLocations = new Dictionary<int, int>();
            foreach (var job in jobs)
            {
                switch (job.Type)
                {
                    case JobTypeEnums.NormalBuild:
                        {
                            var plan = JsonSerializer.Deserialize<NormalBuildPlan>(job.Content);

                            Modify(buildings, plan, changedLocations);
                            job.Content = GetContent(plan);

                            if (IsValidPlan(buildings, plan)) yield return job;
                            continue;
                        }
                    case JobTypeEnums.ResourceBuild:
                        {
                            var plan = JsonSerializer.Deserialize<ResourceBuildPlan>(job.Content);
                            if (IsValidPlan(buildings, plan)) yield return job;
                            continue;
                        }
                    default:
                        continue;
                }
            }
        }

        private static string GetContent(NormalBuildPlan plan)
        {
            return JsonSerializer.Serialize(plan);
        }

        private static bool IsValidPlan(List<BuildingItem> buildings, ResourceBuildPlan plan)
        {
            var fieldTypes = _fieldList[plan.Plan];

            var fields = buildings
                .Where(x => fieldTypes.Contains(x.Type))
                .ToList();

            if (fields.TrueForAll(x => x.Level >= plan.Level)) return false;

            foreach (var field in fields)
            {
                if (field.Level < plan.Level) field.JobLevel = plan.Level;
            }
            return true;
        }

        private static bool IsValidPlan(List<BuildingItem> buildings, NormalBuildPlan plan)
        {
            var building = buildings.Find(x => x.Location == plan.Location);
            if (building is null) return false;

            if (building.Type != BuildingEnums.Site)
            {
                if (building.Type != plan.Type) return false;
                if (building.Level >= plan.Level) return false;
            }
            else
            {
                building.Type = plan.Type;
            }

            building.JobLevel = plan.Level;
            return true;
        }

        private static void Modify(List<BuildingItem> buildings, NormalBuildPlan plan, Dictionary<int, int> changedLocations)
        {
            if (plan.Type.IsResourceField()) return;

            if (plan.Type.IsMultipleBuilding())
            {
                if (ModifyMultiple(buildings, plan)) return;
            }
            else
            {
                if (plan.Type.IsWall())
                {
                    var wall = buildings.Find(x => x.Location == 40);
                    if (plan.Type != wall.Type)
                    {
                        plan.Type = wall.Type;
                    }
                }

                if (ModifySame(buildings, plan)) return;
            }

            if (_excludedLocations.Contains(plan.Location)) return;

            ModifyRandom(buildings, plan, changedLocations);
        }

        private static bool ModifyMultiple(List<BuildingItem> buildings, NormalBuildPlan plan)
        {
            var largestLevelBuilding = buildings
                .Where(x => x.Type == plan.Type)
                .OrderByDescending(x => x.Level)
                .FirstOrDefault();
            if (largestLevelBuilding is not null && largestLevelBuilding.Type.GetMaxLevel() > largestLevelBuilding.Level)
            {
                plan.Location = largestLevelBuilding.Location;
                return true;
            }
            return false;
        }

        private static bool ModifySame(List<BuildingItem> buildings, NormalBuildPlan plan)
        {
            var sameTypeBuilding = buildings
                .Find(x => x.Type == plan.Type);
            if (sameTypeBuilding is not null)
            {
                if (sameTypeBuilding.Location != plan.Location)
                {
                    plan.Location = sameTypeBuilding.Location;
                }
                return true;
            }
            return false;
        }

        private static void ModifyRandom(List<BuildingItem> buildings, NormalBuildPlan plan, Dictionary<int, int> changedLocations)
        {
            var freeLocations = buildings
               .Where(x => x.Type == BuildingEnums.Site)
               .Select(x => x.Location)
               .Where(x => !_excludedLocations.Contains(x))
               .ToList();

            if (!changedLocations.ContainsKey(plan.Location))
            {
                changedLocations[plan.Location] = GetRandomLocation(freeLocations);
            }

            plan.Location = changedLocations[plan.Location];
        }

        private static int GetRandomLocation(List<int> freeLocations)
        {
            return freeLocations[Random.Shared.Next(0, freeLocations.Count - 1)];
        }

        private void AddJobToDatabase(VillageId villageId, IEnumerable<JobDto> jobDtos)
        {
            using var context = _contextFactory.CreateDbContext();
            var count = context.Jobs
               .Where(x => x.VillageId == villageId.Value)
               .Count();

            var jobs = new List<Job>();
            foreach (var jobDto in jobDtos)
            {
                var job = new Job()
                {
                    Position = count,
                    VillageId = villageId.Value,
                    Type = jobDto.Type,
                    Content = jobDto.Content,
                };

                jobs.Add(job);
                count++;
            }

            context.AddRange(jobs);
            context.SaveChanges();
        }
    }
}