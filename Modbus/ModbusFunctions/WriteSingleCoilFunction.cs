using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    public class WriteSingleCoilFunction : ModbusFunction
    {
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters p = CommandParameters as ModbusWriteCommandParameters;
            byte[] request = new byte[12];

            // Transaction ID
            byte[] transactionId = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId));
            request[0] = transactionId[0];
            request[1] = transactionId[1];

            // Protocol ID = 0
            request[2] = 0;
            request[3] = 0;

            // Length = 6
            byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length));
            request[4] = length[0];
            request[5] = length[1];

            // Unit ID
            request[6] = p.UnitId;

            // Function code = 0x05
            request[7] = p.FunctionCode;

            // Adresa coila koji pisemo
            byte[] outputAddress = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.OutputAddress));
            request[8] = outputAddress[0];
            request[9] = outputAddress[1];

            // Vrednost: 0xFF00 = ON, 0x0000 = OFF
            // Korisnik salje 1 ili 0, mi to pretvaramo u Modbus format
            ushort coilValue = (p.Value != 0) ? (ushort)0xFF00 : (ushort)0x0000;
            byte[] value = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)coilValue));
            request[10] = value[0];
            request[11] = value[1];

            return request;
        }

        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusWriteCommandParameters p = CommandParameters as ModbusWriteCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            // Provera greske
            if ((response[7] & 0x80) != 0)
            {
                HandeException(response[8]);
                return result;
            }

            // Iz odgovora izvuci vrednost koja je upisana
            // Bajt 10-11 sadrze vrednost (0xFF00 ili 0x0000)
            ushort rawValue = (ushort)IPAddress.NetworkToHostOrder(
                (short)BitConverter.ToInt16(response, 10)
            );

            // Pretvori nazad u 0 ili 1
            ushort coilState = (rawValue == 0xFF00) ? (ushort)1 : (ushort)0;

            result.Add(
                new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, p.OutputAddress),
                coilState
            );

            return result;
        }
    }
}