using System;
using System.Globalization;
using System.Windows.Data;
using MainCore.Common.Enums;
using MainCore.UI.ViewModels.Tabs.Villages;

namespace WPFUI.Converter
{
    public class BuildingNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name)
            {
                var normalized = name.Replace(" ", "").Replace("_", "").ToLower();

                foreach (BuildingEnums buildingEnum in Enum.GetValues(typeof(BuildingEnums)))
                {
                    var enumName = buildingEnum.ToString().ToLower();
                    if (enumName.Equals(normalized))
                    {
                        return BuildViewModel.GetNameInChinese(buildingEnum);
                    }
                }

                foreach (ResourcePlanEnums resourceEnum in Enum.GetValues(typeof(ResourcePlanEnums)))
                {
                    var enumName = resourceEnum.ToString().ToLower();
                    if (enumName.Equals(normalized))
                    {
                        return BuildViewModel.GetNameInChinese(resourceEnum);
                    }
                }

                foreach (TroopEnums troopEnum in Enum.GetValues(typeof(TroopEnums)))
                {
                    var enumName = troopEnum.ToString().ToLower();
                    if (enumName.Equals(normalized))
                    {
                        return BuildViewModel.GetNameInChinese(troopEnum);
                    }
                }

                foreach (TribeEnums tribeEnum in Enum.GetValues(typeof(TribeEnums)))
                {
                    var enumName = tribeEnum.ToString().ToLower();
                    if (enumName.Equals(normalized))
                    {
                        return BuildViewModel.GetNameInChinese(tribeEnum);
                    }
                }

                return name;
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
