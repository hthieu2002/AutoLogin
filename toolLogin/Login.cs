using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;

namespace toolLogin
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }
        private void RunAdbCommand(string command)
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + command)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process proc = new Process { StartInfo = procStartInfo };
            proc.Start();
            string result = proc.StandardOutput.ReadToEnd();
            Console.Write(result);
            //MessageBox.Show(result);
        }
        private (int x, int y) GetElementCoordinates(string elementText)
        {
            // Lấy thông tin layout của màn hình hiện tại
            RunAdbCommand("adb shell uiautomator dump /sdcard/window_dump.xml");
            RunAdbCommand("adb pull /sdcard/window_dump.xml");

            // Phân tích file XML để tìm tọa độ phần tử cụ thể
            string xmlContent = File.ReadAllText("window_dump.xml");
            var doc = XDocument.Parse(xmlContent);
            var element = doc.Descendants("node")
                             .FirstOrDefault(node => node.Attribute("text")?.Value == elementText);

            if (element != null)
            {
                var bounds = element.Attribute("bounds")?.Value;
                var match = Regex.Match(bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
                if (match.Success)
                {
                    int x1 = int.Parse(match.Groups[1].Value);
                    int y1 = int.Parse(match.Groups[2].Value);
                    int x2 = int.Parse(match.Groups[3].Value);
                    int y2 = int.Parse(match.Groups[4].Value);
                    int x = (x1 + x2) / 2;
                    int y = (y1 + y2) / 2;
                    return (x, y);
                }
            }
            return (0, 0);
        }
        private (int x, int y) WaitForCoordinates(string elementText, int timeout = 30000)
        {
            int elapsed = 0;
            int interval = 1000;

            while (elapsed < timeout)
            {
                var coordinates = GetElementCoordinates(elementText);
                if (coordinates.x != 0 && coordinates.y != 0)
                {
                    return coordinates;
                }

                System.Threading.Thread.Sleep(interval);
                elapsed += interval;
            }

            throw new Exception($"Timeout: Element with text '{elementText}' not found.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text;
            string password = txtPassword.Text;
            RunAdbCommand("adb shell am start -a android.settings.ADD_ACCOUNT_SETTINGS");
            var (a, b) = WaitForCoordinates("Google");
            if (a != 0 && b != 0)
            {
                System.Threading.Thread.Sleep(1000);
                RunAdbCommand($"adb shell input tap 500 800");

                System.Threading.Thread.Sleep(1000);
                RunAdbCommand($"adb shell input text \"{email}\"");
                RunAdbCommand("adb shell input keyevent 66");
                System.Threading.Thread.Sleep(1000);
                RunAdbCommand($"adb shell input text \"{password}\"");
                RunAdbCommand("adb shell input keyevent 66");
                System.Threading.Thread.Sleep(1000);
                (int x, int y) = GetElementCoordinates("I agree");
                if (x != 0 && y != 0)
                {
                    RunAdbCommand($"adb shell input tap {x} {y}");
                }
                else
                {
                    MessageBox.Show("Không tìm thấy nút 'Next'");
                }
            }
        }
	} 
}
