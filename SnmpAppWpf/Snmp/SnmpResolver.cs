using SnmpSharpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SnmpAppWpf.Snmp
{
    internal class SnmpResolver
    {
        public SnmpResolver(string ipAddress, string community)
        {
            this.ipAddress = ipAddress;
            this.community = community;

            GenerateOids();
        }

        private string community = string.Empty;
        private string ipAddress = string.Empty;

        public IList<OidRow> OidInterfaceNumber { get; set; }
        public IList<OidRow> OidInterfaceTable { get; set; }
        public IList<OidRow> OidTable { get; set; }
        public IList<string> Errors { get; set; }

        private void GenerateOids()
        {
            OidInterfaceNumber = new List<OidRow>
            {
                new OidRow()
                {
                    Description = "ifNumber",
                    Oid = "1.3.6.1.2.1.2.1.0"
                }
            };

            OidInterfaceTable = new List<OidRow>
            {
                //new OidRow()
                //{
                //    Description = "if",
                //    Oid = "1.3.6.1.2.1.2.2.1.2.{0}"
                //}
                //new OidRow()
                //{
                //    Description = "if1",
                //    Oid = "1.3.6.1.2.1.2.2.1.2.1"
                //},
                //new OidRow()
                //{
                //    Description = "if2",
                //    Oid = "1.3.6.1.2.1.2.2.1.2.2"
                //}
            };

            OidTable = new List<OidRow>
            {
                new OidRow()
                {
                    Description = "sysDescr",
                    Oid = "1.3.6.1.2.1.1.1.0"
                },
                new OidRow()
                {
                    Description = "sysObjectID",
                    Oid = "1.3.6.1.2.1.1.2.0"
                },
                new OidRow()
                {
                    Description = "sysUpTime",
                    Oid = "1.3.6.1.2.1.1.3.0"
                },
                new OidRow()
                {
                    Description = "sysContact",
                    Oid = "1.3.6.1.2.1.1.4.0"
                },
                new OidRow()
                {
                    Description = "sysName",
                    Oid = "1.3.6.1.2.1.1.5.0"
                },
                new OidRow()
                {
                    Description = "ifInOctets",
                    Mask = "1.3.6.1.2.1.2.2.1.10.{0}"
                },
                new OidRow()
                {
                    Description = "ifOutOctets",
                    Mask = "1.3.6.1.2.1.2.2.1.16.{0}"
                }
            };
        }

        public void Reset()
        {
            foreach (OidRow or in OidTable)
            {
                or.Results.Clear();
            }
        }

        public void SetConfig(string ipAddress, string community)
        {
            this.ipAddress = ipAddress;
            this.community = community;
        }

        private void GetSNMPValues(IList<OidRow> oidTable)
        {
            // SNMP community name
            OctetString communityObj = new OctetString(community);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(communityObj);
            param.Version = SnmpVersion.Ver2;

            IpAddress agent = new IpAddress(ipAddress);

            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 2000, 1);

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.Get);
            foreach (OidRow or in oidTable)
            {
                pdu.VbList.Add(or.Oid);
            }

            // Make SNMP request
            SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
            // If result is null then agent didn't reply or we couldn't parse the reply.
            if (result != null)
            {
                // ErrorStatus other then 0 is an error returned by
                // the Agent - see SnmpConstants for error definitions
                if (result.Pdu.ErrorStatus != 0)
                {
                    // agent reported an error with the request
                    Errors.Add($"Error in SNMP reply. Error {result.Pdu.ErrorStatus} index {result.Pdu.ErrorIndex}. ");
                }
                else
                {
                    DateTime date = DateTime.Now;
                    // Reply variables are returned in the same order as they were added
                    //  to the VbList
                    foreach (OidRow or in oidTable)
                    {
                        or.PreviousResult = or.CurrentResult;
                        or.CurrentResult = result.Pdu.VbList[oidTable.IndexOf(or)].Value.ToString();
                        or.CurrentDate = date;

                        or.Results.Add(new OidResult()
                        {
                            RequestDate = date,
                            Value = or.CurrentResult
                        });
                    }
                }
            }
            else
            {
                Console.WriteLine("No response received from SNMP agent.");
            }
            target.Close();
        }

        public void GetInterfaces()
        {
            GetSNMPValues(OidInterfaceNumber);

            if (OidInterfaceNumber.First().CurrentResult != null)
            {
                OidInterfaceTable.Clear();

                bool success = int.TryParse(OidInterfaceNumber.First().CurrentResult, out int interfacesNumber);
                if (success)
                {
                    string mask = "1.3.6.1.2.1.2.2.1.2.{0}";
                    for (int i = 1; i <= interfacesNumber; i++)
                    {
                        OidInterfaceTable.Add(new OidRow() {
                            Description = $"if{i}",
                            Oid = string.Format(mask, i)
                        });
                    }
                    GetSNMPValues(OidInterfaceTable);
                }
            }
        }

        public void GetInterfaceData(int interfaceId)
        {
            var oidRowIn = OidTable.FirstOrDefault(r => r.Description.Equals("ifInOctets"));
            if (oidRowIn != null)
            {
                oidRowIn.Oid = string.Format(oidRowIn.Mask, interfaceId);
            }
            var oidRowOut = OidTable.FirstOrDefault(r => r.Description.Equals("ifOutOctets"));
            if (oidRowOut != null)
            {
                oidRowOut.Oid = string.Format(oidRowOut.Mask, interfaceId);
            }

            GetSNMPValues(OidTable);
        }
    }

    internal class OidRow
    {
        public string Description { get; set; }
        public string Mask { get; set; }
        public string Oid { get; set; }
        public string PreviousResult { get; set; }
        public string CurrentResult { get; set; }
        public DateTime CurrentDate { get; set; }
        public IList<OidResult> Results { get; set; } = new List<OidResult>();
    }

    internal class OidResult
    {
        public DateTime RequestDate { get; set; }
        public string Value { get; set; }
    }

    internal class InterfacesTable
    {
        public int Number { get; set; }

        public string Description { get; set; }
    }
}
