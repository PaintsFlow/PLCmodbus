using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// NModbus4 패키지
using NModbus;
using NModbus.Device;

namespace VirtualPLCReader
{
    class VirtualPLCReader
    {
        static void Main(string[] args)
        {
            // [1] PLC 접속 정보 (쓰기 때와 동일하게)
            string plcIp = "192.168.10.200";
            int plcPort = 502;   // 기본 ModbusTCP 포트
            byte slaveId = 1;    // PLC에서 설정한 Slave ID

            // 읽어올 시작 주소와 레지스터 개수
            ushort startAddress = 0;
            ushort numRegistersToRead = 10; 

            try
            {
                // 1) TCP 연결
                using (TcpClient client = new TcpClient(plcIp, plcPort))
                {
                    // 2) Modbus Master 생성
                    var factory = new ModbusFactory();
                    IModbusMaster master = factory.CreateMaster(client);

                    Console.WriteLine($"Modbus TCP 연결 성공: {plcIp}:{plcPort}, Slave ID={slaveId}");
                    Console.WriteLine("Reading Holding Registers...\n");

                    // 3) 주기적으로 (무한루프) 데이터 읽기
                    while (true)
                    {
                        // a) PLC의 Holding Register 읽기
                        //    ReadHoldingRegisters(슬레이브ID, 시작주소, 레지스터갯수)
                        ushort[] registers = master.ReadHoldingRegisters(slaveId, startAddress, numRegistersToRead);
                        Console.WriteLine(string.Join(", ", registers));
                        // b) 읽은 ushort[]를 센서값으로 변환
                        //    (쓰는 쪽에서 GenerateRegisterData()와 동일한 포맷으로 해석)
                        int reg0 = registers[0]; 
                        int reg1 = registers[1];
                        int reg2 = registers[2];
                        int reg3 = registers[3];
                        int reg4 = registers[4];
                        int reg5 = registers[5];
                        int reg6 = registers[6];
                        int reg7 = registers[7];
                        int reg8 = registers[8];
                        // reg9 = registers[9]; // 예비/확장

                        // (1) PreTreatment
                        double level     = reg0;        // Level (%)
                        double viscosity = reg1;        // 점도 (cP)
                        double pH        = reg2 / 100.0; // pH는 x100 스케일링
                        double voltage   = reg3;        // 전압 (V)
                        double current   = reg4;        // 전류 (A)

                        // (2) Drying
                        double temperature = reg5;      // 온도 (°C)
                        double humidity    = reg6;      // 습도 (%)

                        // (3) Painting
                        double paintPressure = reg7 / 100.0; // bar는 x100 스케일링
                        double paintFlow     = reg8;         // mL/min

                        // c) 콘솔 출력
                        //Console.WriteLine("=== PreTreatment (하도/전착) ===");
                        //Console.WriteLine($"Level       : {level:F2} %");
                        //Console.WriteLine($"Viscosity   : {viscosity:F2} cP");
                        //Console.WriteLine($"pH          : {pH:F2}");
                        //Console.WriteLine($"Voltage     : {voltage:F2} V");
                        //Console.WriteLine($"Current     : {current:F2} A");
                        //Console.WriteLine();

                        //Console.WriteLine("=== Drying (건조 공정) ===");
                        //Console.WriteLine($"Temperature : {temperature:F2} °C");
                        //Console.WriteLine($"Humidity    : {humidity:F2} %");
                        //Console.WriteLine();

                        //Console.WriteLine("=== Painting (도장 공정) ===");
                        //Console.WriteLine($"PaintPressure : {paintPressure:F2} bar");
                        //Console.WriteLine($"PaintFlow     : {paintFlow:F2} mL/min");
                        //Console.WriteLine("=================================\n");

                        // d) 1초 대기 후 반복
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC 통신 오류: {ex.Message}");
            }
        }
    }
}
