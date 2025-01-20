# Toyota Data Merge

Utility to convert individual .json files out of the [KMG Repo](https://github.com/kissmygritts/flatdata-vehicle-inventory) into a combined .csv file that includes the occurances of each VIN in a new column for `days_available`.

### Requirements:
* .NET Core C# SDK

### Parameters:
* Repo Path - Point to your local clone of the KMG repo
* Filename - inventory.json
* DateSince - Start reding commits from this day forward


### Usage:
* Clone the KMG Repo
* Clone this repo
* Open in VSCode or other IDE
* Adjust your parameters for Repo Path and DateSince
* Open terminal and type `dotnet run`


#### Input Example (inventory.json)

````
[
  {
    "dealer": "02036",
    "vin": "3TYJDAHN3RT015430",
    "year": "2024",
    "vehicle": "tacoma",
    "model": "SR",
    "engine": "i-FORCE 2.4L 4-Cyl. Turbo Engine",
    "transmission": "8-Speed Automatic Transmission",
    "drivetrain": "Rear-Wheel Drive",
    "cab": "XtraCab",
    "bed": "6-ft bed",
    "color": "Ice Cap",
    "interior": "Black Fabric",
    "base_msrp": "31500",
    "total_msrp": "33234.0",
    "availability_date": null,
    "total_packages": 3,
    "packages": "All-Weather Floor Liners[parts_only_msrp], 50 State Emissions, Mudguards",
    "created_at": "2025-01-20 11:26:56"
  },
  {
    "dealer": "02036",
    "vin": "3TYKD5HN2RT017522",
    "year": "2024",
    "vehicle": "tacoma",
    "model": "SR",
    "engine": "i-FORCE 2.4L 4-Cyl. Turbo Engine",
    "transmission": "8-Speed Automatic Transmission",
    "drivetrain": "Rear-Wheel Drive",
    "cab": "Double Cab",
    "bed": "5-ft bed",
    "color": "Underground",
    "interior": "Black Fabric",
    "base_msrp": "33700",
    "total_msrp": "36934.0",
    "availability_date": null,
    "total_packages": 4,
    "packages": "50 State Emissions, SR Upgrade Package, All-Weather Floor Liners[parts_only_msrp], Mudguards",
    "created_at": "2025-01-20 11:26:56"
  }
  ...
]
````



### Output Example

````
dealer,vin,year,vehicle,model,engine,transmission,drivetrain,cab,bed,color,interior,base_msrp,total_msrp,availability_date,total_packages,packages,created_at,days_available
02036,3TYJDAHN3RT015430,2024,tacoma,SR,i-FORCE 2.4L 4-Cyl. Turbo Engine,8-Speed Automatic Transmission,Rear-Wheel Drive,XtraCab,6-ft bed,Ice Cap,Black Fabric,31500,33234.0,,3,"All-Weather Floor Liners[parts_only_msrp], Mudguards, 50 State Emissions",2025-01-17 11:26:55,3
02036,3TYKD5HN2RT017522,2024,tacoma,SR,i-FORCE 2.4L 4-Cyl. Turbo Engine,8-Speed Automatic Transmission,Rear-Wheel Drive,Double Cab,5-ft bed,Underground,Black Fabric,33700,36934.0,,4,"SR Upgrade Package, 50 State Emissions, Mudguards, All-Weather Floor Liners[parts_only_msrp]",2025-01-16 11:26:29,4
...
````