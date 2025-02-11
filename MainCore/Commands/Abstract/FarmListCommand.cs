﻿namespace MainCore.Commands.Abstract
{
    public abstract class FarmListCommand
    {
        protected static IEnumerable<HtmlNode> GetFarmNodes(HtmlDocument doc)
        {
            var raidList = doc.GetElementbyId("rallyPointFarmList");
            if (raidList is null) return Enumerable.Empty<HtmlNode>();
            var fls = raidList
                .Descendants("div")
                .Where(x => x.HasClass("farmListHeader"));
            return fls;
        }

        protected static FarmId GetId(HtmlNode node)
        {
            var flId = node
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("dragAndDrop"));
            if (flId is null) return FarmId.Empty;
            var id = flId.GetAttributeValue("data-list", "0");
            return new FarmId(id.ParseInt());
        }

        protected static string GetName(HtmlNode node)
        {
            var flName = node
                .Descendants("div")
                .FirstOrDefault(x => x.HasClass("name"));
            if (flName is null) return null;
            return flName.InnerText.Trim();
        }
    }
}