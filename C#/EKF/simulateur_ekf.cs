using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Utilities;
using System.Linq;
using System.Threading;
using RobotInterface;
using Constants;
using SciChart.Charting.Visuals;

namespace EKF
{
    public class Simulateur_ekf
    {
        //double LengthTerrain = 0;
        //double WidthTerrain = 0;
        //double vmax = 20;
        //double anglePerceptionRobot = Math.PI;

        static void Main(string[] args)
        {
            // Set this code once in App.xaml.cs or application startup
            // Set this code once in App.xaml.cs or application startup
            //SciChartSurface.SetRuntimeLicenseKey("zCS4B5SF78QhY0ljjdv+82aTvv7lYtUNYMxCVJuCkgvpEB/3Sgy4HFiiA2q6WE83guU54AllcLkHseV1QYLTwWy9+MdLzKbLXS4NXA2yCjUhKg9xpi5V6jxj63gAbsjNGPnDmvB9ElYTJDi9Cz24nw5R8a3O2vY4t6J1INhbgC76F0KU5BjCFhlwmMnzh+0p2ww74KD9ilEcxONSayOH6BdRliHJPrAk1YjXHhu5oI6STAavto5eoAkNYkiA8pUKRz4mPJWNYlrQhILoXCn9NH8ICD5p5ahuJ72V0KyfQpmkT3stY26ZfikKfDKXqqtc7n9GbW4amyhs9VzDCWZpQeh0gL6aBkEml+J1C4dWlZMOSKY7yfB+cwacztebAe2g9V3vDQjh25X03Z6eM6dtdR6eAThSxavKWOFoIQmZcbsh/FqXX9SZsuiznCgXdhsrHHXg5431VnafGPz+r+5GpB6Dy5cv/eqH48TMA4b9tOPGtajr+MvGL+HTz/i80tL1Tw65J0nxWt+430Z21Pm9vQnb+68AucUUTnV9WNOLyatbNlh0WoAt9tDIpGwxlThr6aG0oWX3SQ==");
            //SciChartSurface.SetRuntimeLicenseKey("rviDeI6IkFYHxvazPlQ1Pg9nf76u8PuGK7wRqQ7R9khsGosRUfWiHG6ecygizFR+4c7wUOamApjxFMOAJv2kFhoF82yW1/KvejxkGTqWwFJ5FRYet4s7QSTUnPWuMCSkYUtvd9MBBox4tqaHYq2d9TW8wU0B6wWbgJ7T+XGIO5aW74SZKy+69WnCAbQq/yjjqpaxgSKBNZrApbrkuJupiE69geraLLhedlptkG2Jlvz5EpwJ2O5tOg9IXBP5A7P8YGQmQes9RhuXZuAA4htm4+cshLT7zfegpYm7L5I1zTJOwZNf6wXSFJ/Oa2VgC5It7LSvkuQqDbGhr7IHLPcRrhmplD0bvdM3DgRySzLva+y8ut+8ilI7vFwgRc+3HnEZQVTo92L5LnBkOSHX6IuTS4lw8NLb97WE2aQJnXwR9Apgg8aPxdy8cVmVoCTq35Z1HazmDgeVbp855bZekTNlS+htS/G/DPXNAvnCZlrG063NqDCHRZtRc+xr7RWuicGjPg==");
            //SciChartSurface.SetRuntimeLicenseKey("u83RRb9LnIoaIJdv+T82za7LJQ9T8I6zZSqor2piQl2n1uxCEdXO5Ldo72A84CBQXCR9fLJO9C92eZBV8UmOWqPRrha8zFmMidYoVcafYpP3OrEsqcI7RUlv7FFx0atM95DoYG56R8IFshIbtYX78fLAXE0ZsBt0vRUq/XsSMLtcTziftl+5jaLHf5BrC6Tvvskbxz7kXH2KzWHAzAGJZYDNfls200JOt7flqp1Gd4Lxb0mP2/4YtYeCtoarmoms96LR0WCsz37NhxbW2E2s7QpUjobsMLvy+tX2mq1o4UsyLuUxuVrusqJfo0ofaP0YP/3wVzk3oUXk26o2wHyxDPGBCmFCcXUTt65g7qNOUcKLyRvp86LSVDJjKh3blb2VildUTKqQu5UjAiMtLSRzi2S8SzVPz9mxtnHIPf9RoqamnyrKkvAQZYB7aKKpb3fe1fCcBzcbxDYO/hYxRSgmPiw5JyovZSRPkjJYIUUdOaxa+DlOB7icQDT6imkuLUYuViPrF+Olul74NJvlAviccTFRp0UyMf2QByeuqPEr2JWBOJM2gnFPIrfaPErvuBtyCejh4aERSxREtmj500a8evo=");

        }





    }
}