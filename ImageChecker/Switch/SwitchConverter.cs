using System;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Markup;

namespace ImageChecker.Switch;

/// <summary>
/// A converter that accepts <see cref="SwitchConverterCase"/>s and converts them to the 
/// Then property of the case.
/// </summary>
[ContentProperty("Cases")]
public class SwitchConverter : IValueConverter
{
    #region Public Properties.
    /// <summary>
    /// Gets or sets an array of <see cref="SwitchConverterCase"/>s that this converter can use to produde values from.
    /// </summary>
    public List<SwitchConverterCase> Cases { get; set; }
    public object Default { get; set; }
    #endregion
    #region Construction.
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchConverter"/> class.
    /// </summary>
    public SwitchConverter()
    {
        // Create the cases array.
        Cases = new List<SwitchConverterCase>();
    }
    #endregion

    /// <summary>
    /// Converts a value.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A converted value. If the method returns null, the valid null value is used.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool isSame;

        if (Cases != null && Cases.Count > 0)
            for (int i = 0; i < Cases.Count; i++)
            {
                SwitchConverterCase targetCase = Cases[i];


                if (value == null && targetCase.When == null)
                    return targetCase.Then;

                if (value != null && targetCase.When != null)
                {
                    if ((value.GetType().IsValueType || value.GetType() == typeof(string)) && (targetCase.When.GetType().IsValueType || targetCase.When.GetType() == typeof(string)))
                    {
                        isSame = (value.ToString() == targetCase.When.ToString());

                        // wenn beide numeric sind, dann a - b == 0?
                        if (!isSame && Decimal.TryParse(value.ToString(), out var a) && Decimal.TryParse(targetCase.When.ToString(), out var b))
                        {
                            isSame = (a - b == 0);
                        }
                    }
                    else
                    {
                        isSame = value.Equals(targetCase.When);
                    }

                    if (isSame)
                    {
                        return targetCase.Then;
                    }
                }
            }

        return Default;
    }

    /// <summary>
    /// Converts a value.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// A converted value. If the method returns null, the valid null value is used.
    /// </returns>
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Represents a case for a switch converter.
/// </summary>
[ContentProperty("Then")]
public class SwitchConverterCase
{
    // case instances.
    object _when;
    object _then;

    #region Public Properties.
    /// <summary>
    /// Gets or sets the condition of the case.
    /// </summary>
    public object When { get => _when; set => _when = value; }
    /// <summary>
    /// Gets or sets the results of this case when run through a <see cref="SwitchConverter"/>
    /// </summary>
    public object Then { get => _then; set => _then = value; }
    #endregion
    #region Construction.
    /// <summary>
    /// Switches the converter.
    /// </summary>
    public SwitchConverterCase()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchConverterCase"/> class.
    /// </summary>
    /// <param name="when">The condition of the case.</param>
    /// <param name="then">The results of this case when run through a <see cref="SwitchConverter"/>.</param>
    public SwitchConverterCase(object when, object then)
    {
        // Hook up the instances.
        _then = then;
        _when = when;
    }
    #endregion

    /// <summary>
    /// Returns a <see cref="System.String"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return string.Format("When={0}; Then={1}", When.ToString(), Then.ToString());
    }
}
