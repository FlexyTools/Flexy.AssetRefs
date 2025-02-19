#if !FLEXY_JSONX
namespace Flexy.Serialisation;

public interface ISerializeAsString
{
	public	String		ToString	( );
	public	void		FromString	( String data );
}
#endif