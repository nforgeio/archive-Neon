//-----------------------------------------------------------------------------
// These tests require a Couchbase server as well as a Sync-Gateway.  The server
// requirements are:
//
//		- listening on localhost using the default ports
//		- admin credentials set to: UID=administrator, PWD=test000
//
// To install:
//
//		1. Go to this page: http://www.couchbase.com/nosql-databases/downloads
//		2. Click "Community Edition" under the title.
//		3. Download the Windows (64-bit) version.
//		4. Run the install using the defaults.
//		5. Open the admin UX at http://localhost:8091 in a browser.
//		6. Click SETUP, NEXT, NEXT, NEXT to the NOTIFICATIONS step.  Uncheck
//	       Enable and click NEXT.
//      7. Enter "test000" in both the password boxes.
//      8. Click NEXT.  Your server will be ready in a few seconds.
//      9. Click the DATA BUCKETS tab.
//     10. Click the blue triangle tothe right of the [default] bucket name.
//     11. Click EDIT on the right side.
//     12. Change per node RAM quota to 100MB and SAVE.
//
// The tests will provision a bucket named [unit-test] and write test data there.
//
// The tests also require that Couchbase Sync-Gateway be installed on you Windows
// workstation.
//
//	To install:
//
//		1. Go to this page: http://www.couchbase.com/nosql-databases/downloads
//      2. Click COUCHBASE MOBILE at the top.
//		3. Click "Community Edition" under the title.
//      4. Select version 1.2.0 from the drop-down (1.2.1 does not install)
//		5. Download the Windows version.
//		6. Run the install using the defaults.
//      7. Manually start the Couchbase Sync Gateway service.
