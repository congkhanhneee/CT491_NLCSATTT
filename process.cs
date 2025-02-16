using System;
using System.Text;
using System.Runtime.InteropServices;

class Program
{
    // Import EnumProcesses từ psapi.dll để lấy danh sách các tiến trình
    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumProcesses([Out] uint[] processIds, uint arraySizeBytes, out uint bytesReturned);

    // Import OpenProcess từ kernel32.dll để lấy thông tin tiến trình
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    // Import GetModuleBaseName từ psapi.dll để lấy tên tiến trình
    [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, uint nSize);

    // Import CloseHandle để giải phóng bộ nhớ
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    // Quyền truy cập vào tiến trình
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const uint PROCESS_VM_READ = 0x0010;
    const int MAX_PROCESSES = 1024; // Số lượng tối đa tiến trình
    const int MAX_NAME_LENGTH = 256; // Độ dài tối đa của tên tiến trình

    static void Main()
    {
        uint[] processIds = new uint[MAX_PROCESSES]; // Mảng chứa ID của các tiến trình
        uint bytesReturned;

        // Lấy danh sách ID của tất cả các tiến trình
        if (EnumProcesses(processIds, (uint)(processIds.Length * sizeof(uint)), out bytesReturned))
        {
            int numProcesses = (int)(bytesReturned / sizeof(uint));

            Console.WriteLine("Danh sách tiến trình đang chạy:");
            Console.WriteLine("-------------------------------------");

            for (int i = 0; i < numProcesses; i++)
            {
                uint processId = processIds[i];

                // Mở tiến trình để đọc thông tin
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);

                if (hProcess != IntPtr.Zero)
                {
                    StringBuilder processName = new StringBuilder(MAX_NAME_LENGTH);
                    uint nameLength = GetModuleBaseName(hProcess, IntPtr.Zero, processName, (uint)processName.Capacity);

                    if (nameLength > 0)
                    {
                        Console.WriteLine($"PID: {processId} | Tên: {processName}");
                    }
                    else
                    {
                        Console.WriteLine($"PID: {processId} | Không thể lấy tên tiến trình.");
                    }

                    // Đóng handle để tránh rò rỉ bộ nhớ
                    CloseHandle(hProcess);
                }
            }
        }
        else
        {
            Console.WriteLine("Không thể lấy danh sách tiến trình.");
        }
    }
}
