using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JCBSystem.Core.common.EntityManager
{
    public class RegistryKeys
    {
        private readonly string subKey = @"Software\JCBSystem";

        public T GetRegistLocalSession<T>() where T : class, new()
        {
            var regInfo = new T();

            using (var key = Registry.CurrentUser.OpenSubKey(subKey))
            {
                if (key != null)
                {
                    // Get the type of the object
                    var type = regInfo.GetType();

                    // Loop through all properties of the object
                    foreach (var property in type.GetProperties())
                    {
                        var propertyName = property.Name;

                        try
                        {
                            // Get the protected value from the registry
                            var protectedValue = key.GetValue(propertyName) as string;

                            if (protectedValue != null)
                            {
                                // Unprotect the value
                                var value = protectedValue;

                                // Set the value to the property
                                property.SetValue(regInfo, Convert.ChangeType(value, property.PropertyType));
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log or handle the exception as needed
                            throw new ArgumentException($"Failed to get or set registry value {propertyName}: {ex.Message}");
                        }
                    }
                }
            }

            return regInfo;
        }

        public Task DeleteRegistLocalSession<T>() where T : class, new()
        {
            var regInfo = new T();

            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
            {
                if (key != null)
                {
                    // Get the type of the object
                    var type = regInfo.GetType();

                    // Loop through all properties of the object
                    foreach (var property in type.GetProperties())
                    {
                        // Get the property name
                        var propertyName = property.Name;

                        try
                        {
                            // Delete the key if it exists
                            if (key.GetValue(propertyName) != null)
                            {
                                key.DeleteValue(propertyName, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log or handle the exception as needed
                            throw new KeyNotFoundException($"Failed to delete registry value {propertyName}: {ex.Message}");
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void CreateRegistLocalSession<T>(T regInfo) where T : class
        {
            using (var key = Registry.CurrentUser.CreateSubKey(subKey))
            {
                if (key != null)
                {
                    // Get the type of the object
                    var type = regInfo.GetType();

                    // Loop through all properties of the object
                    foreach (var property in type.GetProperties())
                    {
                        // Get the property name and value
                        var propertyName = property.Name;
                        var propertyValue = property.GetValue(regInfo)?.ToString();

                        // Protect the value before saving
                        var protectedValue = propertyValue;

                        // Save the protected value to the registry
                        key.SetValue(propertyName, protectedValue, RegistryValueKind.String);
                    }
                }
            }
        }


    }
}
