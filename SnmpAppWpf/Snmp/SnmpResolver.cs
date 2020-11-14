using SnmpSharpNet;
using System;
using System.Collections.Generic;
using System.Net;

namespace SnmpAppWpf.Snmp
{
    internal class SnmpResolver
    {
        public SnmpResolver() : this("", "")
        {

        }

        public SnmpResolver(string ipAddress, string community)
        {
            IpAddress = ipAddress;
            Community = community;

            GenerateOids();
        }

        public string Community { get; set; }
        public string IpAddress { get; set; }
        public IList<OidRow> OidTable { get; set; }
        public IList<string> Errors { get; set; }

        private void GenerateOids()
        {
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
                    Oid = "1.3.6.1.2.1.2.2.1.10.1"
                },
                new OidRow()
                {
                    Description = "ifOutOctets",
                    Oid = "1.3.6.1.2.1.2.2.1.16.1"
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

        public void Get()
        {
            // SNMP community name
            OctetString community = new OctetString(Community);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            param.Version = SnmpVersion.Ver2;

            IpAddress agent = new IpAddress(IpAddress);

            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 2000, 1);

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.Get);
            foreach (OidRow or in OidTable)
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
                    foreach (OidRow or in OidTable)
                    {
                        or.PreviousResult = or.CurrentResult;
                        or.CurrentResult = result.Pdu.VbList[OidTable.IndexOf(or)].Value.ToString();
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
    }

    internal class OidRow
    {
        public string Description { get; set; }
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
}
