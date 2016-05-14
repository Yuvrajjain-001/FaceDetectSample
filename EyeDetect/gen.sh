gawk ' BEGIN {

	cIn= 41 * 41
	cOut = 10;
	
	N = 20;
	
	for ( i = 0 ; i < N ; ++i)
	{
		GenSamp(cIn, cOut);
	}
}

function GenSamp (cIn, cOut, i)
{
	printf("{");
	
	for (i = 0 ; i < cIn ; ++i)
	{
		printf (" %d", rand() * 1000);
	}
	
	printf(" } { ");
	
	for (i = 0 ; i < cOut ; ++i)
	{
		printf (" %4.2f", rand());
	}
	
	printf(" }\n");
}

'
	
