using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
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

            // Function code = 0x06
            request[7] = p.FunctionCode;

            // Adresa registra koji pisemo
            byte[] outputAddress = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.OutputAddress));
            request[8] = outputAddress[0];
            request[9] = outputAddress[1];

            // Vrednost koja se upisuje (direktno, bez konverzije)
            byte[] value = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Value));
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

            // Odgovor sadrzi adresu i vrednost koja je upisana
            ushort value = (ushort)IPAddress.NetworkToHostOrder(
                (short)BitConverter.ToInt16(response, 10)
            );

            result.Add(
                new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, p.OutputAddress),
                value
            );

            return result;
        }
    }
}