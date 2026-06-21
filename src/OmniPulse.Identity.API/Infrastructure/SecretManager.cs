using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Diagnostics;

namespace OmniPulse.Identity.API.Infrastructure;

/// <summary>
/// Sistem şifrelerini platforma duyarlı olarak yöneten sınıf. 🐉🛡️
/// Linux (Bazzite) üzerinde systemd credentials altyapısını, Windows üzerinde ise Credential Manager'ı kullanır.
/// </summary>
public static class SecretManager
{
    // Windows Credential Manager P/Invoke Tanımları
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public int Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr buffer);

    public static string GetSecret(string secretName)
    {
        // 1. DÜNYA: Bazzite (Linux) Üzerindeysek 🐧
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // systemd'nin şifreleri çözüp koda sunduğu o özel klasör yolu:
            var creedsPath = Environment.GetEnvironmentVariable("CREDENTIALS_DIRECTORY");
            
            if (!string.IsNullOrEmpty(creedsPath))
            {
                // Parametre olarak gelen isme göre dosyayı buluyoruz (örn: omnipulse-secrets)
                var filePath = Path.Combine(creedsPath, secretName);
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath).Trim();
                }
            }

            // Geliştirme kolaylığı: Doğrudan /etc/ altındaki şifreli dosyaları çöz
            // Disk üzerindeki dosya isimleri ile kod içindeki isimler eşleştiriliyor:
            var systemCredPath = secretName switch
            {
                "jwt-key" => "/etc/omnipulse-jwt.cred",
                "postgres-key" => "/etc/omnipulse-postgres.cred",
                "cosmos-db-key" => "/etc/omnipulse-secrets.cred",
                _ => $"/etc/{secretName}.cred"
            };

            if (File.Exists(systemCredPath))
            {
                var decrypted = DecryptSystemdCredential(systemCredPath);
                if (decrypted != null) return decrypted;
            }

            // Kullanıcı dizininde alternatif yollar
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var baseDir = Path.Combine(userHome, ".config", "omnipulse");
            var localCredPath = secretName switch
            {
                "jwt-key" => Path.Combine(baseDir, "omnipulse-jwt.cred"),
                "postgres-key" => Path.Combine(baseDir, "omnipulse-postgres.cred"),
                "cosmos-db-key" => Path.Combine(baseDir, "omnipulse-secrets.cred"),
                _ => Path.Combine(baseDir, $"{secretName}.cred")
            };

            if (File.Exists(localCredPath))
            {
                var decrypted = DecryptSystemdCredential(localCredPath, isUserScope: true);
                if (decrypted != null) return decrypted;
            }

            // Çevre Değişkeni Fallback
            var envName = secretName.ToUpperInvariant().Replace("-", "_").Replace(".", "_");
            var envValue = Environment.GetEnvironmentVariable(envName) ??
                           Environment.GetEnvironmentVariable(secretName) ??
                           Environment.GetEnvironmentVariable(secretName.Replace("-", "_")) ??
                           Environment.GetEnvironmentVariable(secretName.ToUpperInvariant());

            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // Rider/Geliştirici dostu hata teşhis logları
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[SecretManager] HATA: '{secretName}' sırrı kasadan okunamadı ve Çevre Değişkenlerinde (Env) bulunamadı.");
            Console.WriteLine($"Aranan isim varyasyonları: '{envName}', '{secretName}', '{secretName.Replace("-", "_")}', '{secretName.ToUpperInvariant()}'");
            Console.WriteLine("Mevcut Çevre Değişkenleri (İlk 15 anahtar):");
            var envCount = 0;
            foreach (var de in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Where(_ => envCount++ < 15))
            {
                Console.WriteLine($"  -> {de.Key}");
            }
            Console.WriteLine();
            Console.ResetColor();

            throw new Exception($"Bazzite üzerinde '{secretName}' credential dosyası veya çevre değişkeni bulunamadı, Serkan-sama!");
        }
        
        // 2. DÜNYA: İleride Windows'ta Açarsan 🪟
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows Credential Manager'da ararken de isim uyumluluğunu destekleyelim (Örn: omnipulse-jwt veya OmniPulse_JWT)
            if (!CredRead(secretName, 1, 0, out var credPtr) &&
                !CredRead(secretName.Replace("-", "_"), 1, 0, out credPtr)) return "Windows_Lokal_Test_Sifresi_Gelecek";
            try
            {
                var cred = Marshal.PtrToStructure<Credential>(credPtr);
                if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
                {
                    byte[] blob = new byte[cred.CredentialBlobSize];
                    Marshal.Copy(cred.CredentialBlob, blob, 0, cred.CredentialBlobSize);
                    return Encoding.Unicode.GetString(blob);
                }
            }
            finally
            {
                CredFree(credPtr);
            }

            // Windows Credential Manager'da bulunamazsa test/varsayılan değerini döndür
            return "Windows_Lokal_Test_Sifresi_Gelecek"; 
        }
        
        throw new PlatformNotSupportedException("Bu işletim sistemi desteklenmiyor!");
    }

    private static string? DecryptSystemdCredential(string credentialPath, bool isUserScope = false)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "systemd-creds",
                Arguments = isUserScope 
                    ? $"--user decrypt \"{credentialPath}\""
                    : $"decrypt \"{credentialPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    return output;
                }
            }
        }
        catch
        {
            // Hata durumunda null dönerek bir sonraki fallback adıma izin ver
        }

        return null;
    }
}
