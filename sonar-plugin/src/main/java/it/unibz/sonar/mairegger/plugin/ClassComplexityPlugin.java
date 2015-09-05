package it.unibz.sonar.mairegger.plugin;

import org.sonar.api.SonarPlugin;
import org.sonar.api.Extension;
import java.util.ArrayList;
import java.util.List;

public class ClassComplexityPlugin extends SonarPlugin {
    public final List<Class<? extends Extension>> getExtensions()
  {
    List<Class<? extends Extension>> list = new ArrayList();
    
    list.add(ClassComplexityWidget.class);
    
    return list;
  }
}