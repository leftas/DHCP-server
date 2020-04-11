using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace DNS_Server.ValidationRules
{
    public class IPv4ValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value,
          CultureInfo cultureInfo)
        {
            var str = this. GetBoundValue(value) as string;
            if (string.IsNullOrEmpty(str))
            {
                return new ValidationResult(false,
                  "Please enter an IP Address.");
            }

            var parts = str.Split('.');
            if (parts.Length != 4)
            {
                return new ValidationResult(false,
                  "IP Address should be four octets, separated by decimals.");
            }

            foreach (var p in parts)
            {
                if (!int.TryParse(p, NumberStyles.Integer,
                  cultureInfo.NumberFormat, out int intPart))
                {
                    return new ValidationResult(false,
                      "Each octet of an IP Address should be a number.");
                }

                if (intPart < 0 || intPart > 255)
                {
                    return new ValidationResult(false,
                      "Each octet of an IP Address should be between 0 and 255.");
                }
            }

            return new ValidationResult(true, null);
        }

        private object GetBoundValue(object value)
        {
            if (value is BindingExpression binding)
            {
                object dataItem = binding.DataItem;

                string propertyName = binding.ParentBinding.Path.Path;

                return dataItem.GetType().GetProperty(propertyName).GetValue(dataItem, null);
            }
            else
            {
                return value;
            }
        }
    }
}
