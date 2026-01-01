namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class SetUniversalVersion : IPipelineTask
{
	[SerializeField]	String?		_versionTag;
	[SerializeField]	Int32		_n;
	[SerializeField]	ETimeSource	_timeSource;
	[SerializeField]	EMajorType	_major;
	[SerializeField]	EMinorType	_minor;
	[SerializeField]	EBuildTime	_buildTime;
	
	[Header( "Build Number")]
	[SerializeField]	Boolean		_setBuildNumber = true;
	[SerializeField]	Int32		_buildNumberOffset;

	public void Run( Pipeline ppln, Context ctx )
	{
		var buildArtifactsDir	= "Assets/Resources/Fun.Flexy/BuildArtifacts";
		var versionTagPath		= buildArtifactsDir + "/VersionTag.txt";
		
		if (!Directory.Exists(buildArtifactsDir))
			Directory.CreateDirectory(buildArtifactsDir);
		
		if (!String.IsNullOrWhiteSpace(_versionTag))
		{
			File.WriteAllText(versionTagPath, _versionTag);
			AssetDatabase.ImportAsset(versionTagPath, ImportAssetOptions.ForceSynchronousImport);
		}
		
		var time = _timeSource switch
		{
			ETimeSource.UTC		=> DateTime.UtcNow, 
			ETimeSource.Local	=> DateTime.Now,
			_					=> DateTime.UtcNow
		};
		
		var major = _major switch
		{
			EMajorType.YearMinusN	=> time.Year - _n,
			EMajorType.N			=> _n,
			_						=> _n
		};
		
		var minor = "";
		switch (_minor)
		{
			case EMinorType.MonthAndDay:
			{ 
				minor = $"{time.Month:00}{time.Day:00}"; 
				break;
			}
			case EMinorType.QuarterAndDay:
			{ 
				var quarter = (time.Month-1)/3+1;
				var day		= Math.Min(time.Day, 30) + (time.Month-1) % 3 * 30;
				minor		= $"{quarter:0}{day:00}"; 
				break;
			}
			case EMinorType.SeasonAndDay:
			{
				var season	= time.Month switch { 1 => 1, 2 => 1, 3 => 2, 4 => 2, 5 => 2, 6 => 3, 7 => 3, 8 => 3, 9 => 4, 10 => 4, 11 => 4, 12 => 5 };
				var day		= Math.Min(time.Day, 30) + (time.Month < 3 ? time.Month-1 : (time.Month-3) % 3) * 30;
				minor		= $"{season:0}{day:00}";
				break;
			}
		}

		var buildTime = _buildTime switch
		{
			EBuildTime.Hours					=> $"{time.Hour + (time.Day == 31 ? 50 : 0):00}",
			EBuildTime.HoursAnd10th				=> $"{time.Hour + (time.Day == 31 ? 50 : 0):00}{Mathf.RoundToInt(time.Minute / 6f):0}",
			EBuildTime.QuartersOfHours			=> time.Day == 31 ? $"99" : $"{time.Hour*4 + time.Minute/15:00}",
			EBuildTime.QuartersOfHoursAnd10th	=> time.Day == 31 ? $"99{time.Hour/2.4f:0}" : $"{time.Hour*4 + time.Minute/15:00}{Mathf.RoundToInt(time.Minute % 15 / 1.5f)}",
		};
		
		var versionString = $"{major}.{minor}.{buildTime}";

		PlayerSettings.bundleVersion				= versionString;
		
		if (_setBuildNumber)
		{
			var buildNumber = Int32.Parse(versionString.Replace(".", "")) + _buildNumberOffset;
			
			PlayerSettings.Android.bundleVersionCode	= buildNumber;
			PlayerSettings.macOS.buildNumber			= buildNumber.ToString();
			PlayerSettings.tvOS.buildNumber				= buildNumber.ToString();
			PlayerSettings.iOS.buildNumber				= buildNumber.ToString();
		}
		
		if (String.IsNullOrWhiteSpace(_versionTag))
			Debug.Log( $"[Set Universal Version] - Setting universal version {versionString}" );
		else
			Debug.Log( $"[Set Universal Version] - Setting universal version {versionString} and tag {_versionTag}" );
	}

	public enum ETimeSource : byte
	{
		UTC,
		Local,
	}
	public enum EMajorType : byte
	{
		N,
		YearMinusN,
	}
	public enum EMinorType : byte
	{
		MonthAndDay,
		QuarterAndDay,
		SeasonAndDay,
	}
	public enum EBuildTime : byte
	{
		Hours,
		QuartersOfHours,
		HoursAnd10th,
		QuartersOfHoursAnd10th,
	}
}

