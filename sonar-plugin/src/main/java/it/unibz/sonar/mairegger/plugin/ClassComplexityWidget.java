package it.unibz.sonar.mairegger.plugin;

import org.sonar.api.SonarPlugin;

import java.util.Arrays;
import java.util.List;
import org.sonar.api.web.*;

import org.sonar.api.web.WidgetScope;

@WidgetCategory({"Global","Complexity"})
@WidgetScope(WidgetScope.GLOBAL)
@Description("This plugin provides a scatterplot of two selected metrics.")
@UserRole(UserRole.USER)
@WidgetProperties(
{
	@WidgetProperty(
		key="metric1",
		description="This is a mandatory parameter",
		type=WidgetPropertyType.METRIC,
		optional=false
  ),
  
	@WidgetProperty(
		key="metric2",
		description="This is a mandatory parameter",
		type=WidgetPropertyType.METRIC,
		optional=false
  )//,
//  @WidgetProperty(
//		key="chartType",
//		description="Select chart type",
//		type=WidgetPropertyType.SINGLE_SELECT_LIST,
//		optional=false,
//		options=
//		{
//			
//			"scatterplot",
//			"sunburst chart",
//			"chord diagram",
//			"treemap",
//			"parallel coordinates",
//			"liquid fill gauge",
//			"line chart",
//			"bar chart",
//			"stacked bar chart",
//			"normalized bar chart",
//			"piechart",
//			"timeseries",
//			"multi series chart"
//		}
//  )
})
public class ClassComplexityWidget extends AbstractRubyTemplate implements RubyRailsWidget {
    public String getId() {
        return "scatterplot";
    }
    public String getTitle() {
        return "Scatter plot";
    }
    protected String getTemplatePath() {
        // uncomment next line for change reloading during development
        //return "path to erb file for developing reasons.";
        return "/Complexity.html.erb";
    }
}