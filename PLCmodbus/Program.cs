//using System;
//using System.Net.Sockets;
//using NModbus;

//class Program
//{
//    static void Main()
//    {
//        string plcIp = "192.168.10.200"; // PLC IP 주소
//        int port = 502; // Modbus TCP 기본 포트 (설정 확인)

//        try
//        {
//            using (TcpClient client = new TcpClient(plcIp, port))
//            {
//                var factory = new ModbusFactory();
//                var master = factory.CreateMaster(client);

//                byte slaveId = 1; // 일반적으로 Modbus TCP는 1 사용
//                ushort startAddress = 2; // U0000 레지스터를 Modbus 주소 0으로 설정
//                ushort[] sensorData = { 1 }; // 입력할 값

//                master.WriteMultipleRegisters(slaveId, startAddress, sensorData);
//                Console.WriteLine($"PLC (Modbus 주소 {startAddress})에 데이터 {sensorData[0]} 전송 완료!");
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"PLC 통신 오류: {ex.Message}");
//        }
//    }
//}
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO.Ports;

// NModbus4 패키지
using NModbus;
using NModbus.Device;

namespace VirtualPLC
{
    class VirtualPLC
    {
        private Random random;

        // [1] PreTreatment(하도/전착 공정) 센서 변수
        private double level;          // 수위(%)
        private double viscosity;      // 점도(cP)
        private double pH;            // pH
        private double voltage;        // 전압(V)
        private double current;        // 전류(A)

        // [2] Drying(건조 공정) 센서 변수
        private double temperature;    // 온도(°C)
        private double humidity;       // 습도(%)

        // [3] Painting(도장 공정) 센서 변수
        private double paintPressure;  // 페인트 압력(bar)
        private double paintFlow;      // 페인트 유량(mL/min)

        static string TestMessage;

        private TcpClient client;
        private IModbusMaster master;  // NModbus4의 Modbus Master
        private bool isRunning;

        /// <summary>
        /// 생성자: 센서값 초기화
        /// </summary>
        public VirtualPLC()
        {
            random = new Random();

            // (초기값) 권장 범위 대략적 랜덤
            // [1] PreTreatment
            level = random.Next(80, 91);              // 80~90%
            viscosity = random.Next(100, 301);        // 100~300 cP
            pH = 5.5 + (random.NextDouble() * 0.5);   // 5.5~6.0
            voltage = 200 + (random.NextDouble() * 100); // 200~300 V
            current = 400 + (random.NextDouble() * 400); // 400~800 A

            // [2] Drying
            temperature = 80 + (random.NextDouble() * 80); // 80~160°C
            humidity = 40 + (random.NextDouble() * 20);    // 40~60%

            // [3] Painting
            paintPressure = 1.4 + (random.NextDouble() * 1.0); // 1.4~2.4 bar
            paintFlow = 200 + (random.NextDouble() * 400);     // 200~600 mL/min
        }

        /// <summary>
        /// Modbus TCP 서버(LS PLC)에 연결 (Slave ID는 PLC 설정값에 따라 조정)
        /// </summary>
        public void ConnectToServer(string serverIp, int port, byte slaveId = 1)
        {
            try
            {
                // 1) PLC(IP: serverIp, Port: port)와 TcpClient로 연결
                client = new TcpClient(serverIp, port);

                // 2) NModbus Factory를 이용해 Master 생성
                var factory = new ModbusFactory();
                master = factory.CreateMaster(client);

                isRunning = true;
                Console.WriteLine($"Modbus TCP 연결 성공: {serverIp}:{port}, Slave ID={slaveId}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect: {ex.Message}");
                Console.WriteLine("Retrying connection in 5 seconds...");
                Thread.Sleep(5000);
                // 재시도
                ConnectToServer(serverIp, port, slaveId);
            }
        }

        /// <summary>
        /// 각 공정 센서값을 난수 기반으로 업데이트
        /// </summary>
        public void UpdateProcesses()
        {
            // [1] PreTreatment
            // Level: 80~90% ±2
            level += (random.NextDouble() * 4 - 2);
            level = Math.Max(70, Math.Min(95, level));

            // Viscosity: 100~300 cP ±20
            viscosity += (random.NextDouble() * 40 - 20);
            viscosity = Math.Max(80, Math.Min(320, viscosity));

            // pH: 5.5~6.0 ±0.05
            pH += (random.NextDouble() * 0.1 - 0.05);
            pH = Math.Max(5.2, Math.Min(6.3, pH));

            // Voltage: 200~300 V ±10
            voltage += (random.NextDouble() * 20 - 10);
            voltage = Math.Max(180, Math.Min(320, voltage));

            // Current: 400~800 A ±50
            current += (random.NextDouble() * 100 - 50);
            current = Math.Max(300, Math.Min(900, current));

            // [2] Drying
            // Temp: 80~160°C ±5
            temperature += (random.NextDouble() * 10 - 5);
            temperature = Math.Max(15, Math.Min(190, temperature));

            // Humidity: 40~60% ±5
            humidity += (random.NextDouble() * 10 - 5);
            humidity = Math.Max(30, Math.Min(70, humidity));

            // [3] Painting
            // paintPressure: 1.4~2.4 bar ±0.2
            paintPressure += (random.NextDouble() * 0.4 - 0.2);
            paintPressure = Math.Max(0.4, Math.Min(2.2, paintPressure));

            // paintFlow: 200~600 mL/min ±50
            paintFlow += (random.NextDouble() * 100 - 50);
            paintFlow = Math.Max(100, Math.Min(650, paintFlow));
        }

        /// <summary>
        /// 현재 센서상태 콘솔 출력 (원하면 주석 해제)
        /// </summary>
        public void DisplayStatus()
        {
            Console.WriteLine("=== PreTreatment (하도/전착) ===");
            Console.WriteLine($"Level       : {level:F2} %");
            Console.WriteLine($"Viscosity   : {viscosity:F2} cP");
            Console.WriteLine($"pH          : {pH:F2}");
            Console.WriteLine($"Voltage     : {voltage:F2} V");
            Console.WriteLine($"Current     : {current:F2} A");
            Console.WriteLine();
            Console.WriteLine("=== Drying (건조 공정) ===");
            Console.WriteLine($"Temperature : {temperature:F2} °C");
            Console.WriteLine($"Humidity    : {humidity:F2} %");
            Console.WriteLine();
            Console.WriteLine("=== Painting (도장 공정) ===");
            Console.WriteLine($"PaintPressure : {paintPressure:F2} bar");
            Console.WriteLine($"PaintFlow     : {paintFlow:F2} mL/min");
            Console.WriteLine($"TestMessage   : {TestMessage}");
            Console.WriteLine("=================================\n");
        }

        /// <summary>
        /// 10개의 레지스터(Word) 값을 생성하여 반환
        /// (WriteMultipleRegisters에 사용)
        /// </summary>
        private ushort[] GenerateRegisterData()
        {
            // 각 센서값을 int로 변환(필요 시 스케일링) 후 ushort 범위 처리
            ushort[] registers = new ushort[10];
           

            int reg0 = (int)Math.Round(level);             // 수위 %
            int reg1 = (int)Math.Round(viscosity);         // 점도 cP
            // pH는 5~6 사이이므로 x100
            int reg2 = (int)Math.Round(pH * 100);
            int reg3 = (int)Math.Round(voltage);           // 전압(V)
            int reg4 = (int)Math.Round(current);           // 전류(A)
            int reg5 = (int)Math.Round(temperature);       // 온도(°C)
            int reg6 = (int)Math.Round(humidity);          // 습도(%)
            // bar -> x100
            int reg7 = (int)Math.Round(paintPressure * 100);
            int reg8 = (int)Math.Round(paintFlow);         // mL/min

            // 나머지 하나는 필요하다면 예비/확장용, 우선 0으로
            int reg9 = 0;

            // int -> ushort 범위(0~65535) 보정
            // 만약 음수가 될 여지가 없다면 그대로 캐스팅
            registers[0] = (ushort)(reg0 < 0 ? 0 : reg0);
            registers[1] = (ushort)(reg1 < 0 ? 0 : reg1);
            registers[2] = (ushort)(reg2 < 0 ? 0 : reg2);
            registers[3] = (ushort)(reg3 < 0 ? 0 : reg3);
            registers[4] = (ushort)(reg4 < 0 ? 0 : reg4);
            registers[5] = (ushort)(reg5 < 0 ? 0 : reg5);
            registers[6] = (ushort)(reg6 < 0 ? 0 : reg6);
            registers[7] = (ushort)(reg7 < 0 ? 0 : reg7);
            registers[8] = (ushort)(reg8 < 0 ? 0 : reg8);
            registers[9] = (ushort)(reg9 < 0 ? 0 : reg9);

            Console.WriteLine(string.Join(", ", registers));

            return registers;
        }

        /// <summary>
        /// Modbus Master를 통해 PLC Holding Register에 쓰기
        /// </summary>
        public void SendData(byte slaveId = 1, ushort startAddress = 0)
        {
            try
            {
                while (isRunning && client != null && client.Connected)
                {
                    // 1) 센서값 갱신
                    UpdateProcesses();

                    // 2) 콘솔 출력(주석 해제 가능)
                    DisplayStatus();

                    // 3) 레지스터 배열(ushort[]) 생성
                    ushort[] registerData = GenerateRegisterData();

                    // 4) Modbus WriteMultipleRegisters (기능 코드 16)
                    //    slaveId = PLC의 Slave ID
                    //    startAddress = PLC의 시작 주소
                    master.WriteMultipleRegisters(slaveId, startAddress, registerData);

                    Console.WriteLine("Modbus WriteMultipleRegisters 성공\n");
                    Thread.Sleep(1000); // 1초 간격
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during data transmission: {ex.Message}");
                isRunning = false;
            }
        }

        private void LogError(string message)
        {
            File.AppendAllText("error.log", $"{DateTime.Now}: {message}\n");
        }
        static SerialPort serialPort;
        static void Main(string[] args)
        {
            var plc = new VirtualPLC();

            // 접속 정보 (IP, Port, Slave ID) 실제 PLC에 맞춰 조정
            string serverIp = "192.168.10.200";
            int serverPort = 502;       // Modbus TCP 기본 포트는 502
            byte slaveId = 1;          // PLC에서 설정한 Slave ID

            serialPort = new SerialPort("COM5", 9600);
            serialPort.DataReceived += SerialPort_DataReceived;

            serialPort.Open();
            // 1) Modbus TCP 연결
            plc.ConnectToServer(serverIp, serverPort, slaveId);

            // 2) 쓰기 쓰레드 시작
            Thread sendDataThread = new Thread(() => plc.SendData(slaveId, 0));
            sendDataThread.Start();
        }
        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = serialPort.ReadLine(); // 아두이노에서 한 줄 읽기
            TestMessage = data;
        }
    }
}
