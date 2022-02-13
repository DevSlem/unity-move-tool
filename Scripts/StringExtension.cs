namespace KgmSlem.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// Change the variable name to the display name for inspector.
        /// </summary>
        /// <returns>display name for inspector</returns>
        public static string InspectorLabel(this string variableName)
        {
            if (variableName == null)
                throw new System.NullReferenceException();

            if (variableName.Length == 0)
                return string.Empty;

            string temp = variableName.Trim();
            temp = char.ToUpper(temp[0]) + temp.Substring(1); // capitalize the first letter

            // If it changes small letter to capital letter, insert a space.
            for (int i = 0; i < temp.Length - 1; i++)
            {
                if (!char.IsLetterOrDigit(temp[i]) || !char.IsLetterOrDigit(temp[i + 1]))
                    continue;

                if ((char.IsLower(temp[i]) && !char.IsLower(temp[i + 1])) || (!char.IsUpper(temp[i]) && char.IsUpper(temp[i + 1])))
                {
                    temp = temp.Insert(i + 1, " ");
                }
            }

            return temp;
        }
    }
}