# Public demo database

This directory creates a SQL Server database named HospitalSms from scratch.
The schema mirrors HospitalSmsDbContext and includes the objects required by the
SMS test-send flow.

The seed contains fictional demo records using names such as Maccabi, Assuta,
Meuhedet and Leumit. These records are examples only. They contain no patient
information, real credentials or real recipient phone numbers, and do not imply
affiliation with those organizations.

## Included

- 20 EF-mapped tables with matching names, columns, SQL types and primary keys.
- sms_unit_category_v.
- sms_test_phone_whitelist.
- sp_sms_dev_send_delta_queue_add.
- Deterministic projects, hospitals, units, categories, templates, rules,
  placeholders and multilingual message content.
- Reserved 555 demo phone numbers only.

The legacy queue tables retain the column name password for schema
compatibility. The only value the demo procedure can write is the literal
NOT_A_REAL_SECRET.

## Create on SQL Server LocalDB

From the service repository:

~~~powershell
.\Database\Initialize-LocalDatabase.ps1
~~~

Or run the SQL files directly:

~~~powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i ".\Database\HospitalSmsDemo.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -b -i ".\Database\Verify-DemoDatabase.sql"
~~~

The creation script deliberately stops if a database named HospitalSms already
exists. It never drops or overwrites a database.

## Application connection string

~~~text
Server=(localdb)\MSSQLLocalDB;Database=HospitalSms;Integrated Security=SSPI;TrustServerCertificate=True;
~~~

For another SQL Server instance, override
ConnectionStrings__DefaultConnection without committing the value.
