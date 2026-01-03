using System;
using System.Collections.Generic;
using Market.Models; // dla RentalRequestStatus

namespace Market.ViewModels
{

        public class PropertyDatesVM
        {
            public int PropertyId { get; set; }
            public string Address { get; set; } = "";
            public List<Row> Items { get; set; } = new();
            public class Row
            {
                public int RequestId { get; set; }
                public string UserId { get; set; } = "";
                public string UserName { get; set; } = "";
                public DateOnly StartDate { get; set; }
                public DateOnly EndDate { get; set; }
                public RentalRequestStatus Status { get; set; }
                public DateTime CreatedUtc { get; set; }
            }
        }
    }