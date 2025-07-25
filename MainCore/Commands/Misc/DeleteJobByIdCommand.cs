﻿namespace MainCore.Commands.Misc
{
    [Handler]
    public static partial class DeleteJobByIdCommand
    {
        public sealed record Command(JobId JobId) : ICommand;

        private static async ValueTask HandleAsync(
            Command command,
            AppDbContext context
            )
        {
            await Task.CompletedTask;
            var jobId = command.JobId;

            var job = context.Jobs
                .Where(x => x.Id == jobId.Value)
                .Select(x => new
                {
                    x.VillageId,
                    x.Position
                })
                .FirstOrDefault();

            if (job is null) return;

            context.Jobs
                .Where(x => x.Id == jobId.Value)
                .ExecuteDelete();

            context.Jobs
                .Where(x => x.VillageId == job.VillageId)
                .Where(x => x.Position > job.Position)
                .ExecuteUpdate(x => x.SetProperty(x => x.Position, x => x.Position - 1));
        }
    }
}