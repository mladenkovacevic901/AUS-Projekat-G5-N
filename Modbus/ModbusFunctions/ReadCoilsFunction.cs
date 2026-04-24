using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    public class ReadCoilsFunction : ModbusFunction
    {
        public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters p = CommandParameters as ModbusReadCommandParameters;
            byte[] request = new byte[12];

            // Transaction ID (2 bajta, big endian)
            byte[] transactionId = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId));
            request[0] = transactionId[0];
            request[1] = transactionId[1];

            // Protocol ID = uvek 0
            request[2] = 0;
            request[3] = 0;

            // Length = 6
            byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length));
            request[4] = length[0];
            request[5] = length[1];

            // Unit ID (adresa uredjaja)
            request[6] = p.UnitId;

            // Function code = 0x01
            request[7] = p.FunctionCode;

            // Pocetna adresa
            byte[] startAddress = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.StartAddress));
            request[8] = startAddress[0];
            request[9] = startAddress[1];

            // Kolicina registara
            byte[] quantity = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Quantity));
            request[10] = quantity[0];
            request[11] = quantity[1];

            return request;
        }

        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            ModbusReadCommandParameters p = CommandParameters as ModbusReadCommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            // Ako je doslo do greske (bit 7 function code-a je 1)
            if ((response[7] & 0x80) != 0)
            {
                HandeException(response[8]);
                return result;
            }

            // Raspakuj svaki coil iz odgovora
            for (int i = 0; i < p.Quantity; i++)
            {
                int byteIndex = 9 + (i / 8);
                int bitIndex = i % 8;
                ushort value = (ushort)((response[byteIndex] >> bitIndex) & 0x01);
                result.Add(
                    new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, (ushort)(p.StartAddress + i)),
                    value
                );
            }

            return result;
        }
    }
}