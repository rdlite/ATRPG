using System;
using System.Collections.Generic;

public class ServicesContainer : Singleton<ServicesContainer> {
    private Dictionary<Type, IService> _systems = new Dictionary<Type, IService>();

    public void Set<TSystem>(TSystem implementedSystem) where TSystem : IService {
        _systems.Add(typeof(TSystem), implementedSystem);
    }

    public TSystem Get<TSystem>() where TSystem : class, IService {
        return _systems[typeof(TSystem)] as TSystem;
    }
}