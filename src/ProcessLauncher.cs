using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace MinecraftHotbar
{
    internal static class ProcessLauncher
    {
        public static void Launch(string target, string name = "")
        {
            if (string.IsNullOrWhiteSpace(target)) return;

            try
            {
                string expanded = Environment.ExpandEnvironmentVariables(target.Trim());

                // Protocolos o URLs web (http, https, spotify, discord, steam, etc.)
                if (expanded.Contains("://") || 
                    expanded.StartsWith("spotify:", StringComparison.OrdinalIgnoreCase) ||
                    expanded.StartsWith("discord:", StringComparison.OrdinalIgnoreCase) ||
                    expanded.StartsWith("steam:", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = expanded,
                        UseShellExecute = true
                    });
                    return;
                }

                // Accesos especiales de Windows Shell (ej. shell:Downloads)
                if (expanded.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = expanded,
                        UseShellExecute = true
                    });
                    return;
                }

                // Si es un directorio existente, abrirlo en el Explorador
                if (Directory.Exists(expanded))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{expanded}\"",
                        UseShellExecute = true
                    });
                    return;
                }

                // Comando estándar o ejecutable
                var psi = new ProcessStartInfo
                {
                    FileName = expanded,
                    UseShellExecute = true
                };

                // Si tiene argumentos separados (ej. powershell.exe -NoExit)
                if (!File.Exists(expanded) && expanded.Contains(" "))
                {
                    int spaceIdx = expanded.IndexOf(' ');
                    string cmd = expanded.Substring(0, spaceIdx);
                    string args = expanded.Substring(spaceIdx + 1);

                    psi.FileName = cmd;
                    psi.Arguments = args;
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo abrir '{name}':\n\n{ex.Message}\n\nRuta configurada: {target}",
                    "Minecraft Desktop Hotbar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }
    }
}
