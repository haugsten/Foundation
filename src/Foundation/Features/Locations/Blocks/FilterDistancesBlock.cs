﻿using EPiServer.DataAnnotations;
using EPiServer.Find;
using EPiServer.Find.Api.Facets;
using EPiServer.Find.Framework;
using Foundation.Features.Shared;
using Foundation.Infrastructure;
using Foundation.Infrastructure.Find;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Foundation.Features.Locations.Blocks
{
    [ContentType(DisplayName = "Filter Distances Block",
        GUID = "eab40a8c-9006-4766-a87e-1dec153e735f",
        Description = "Distance facets for locations",
        GroupName = GroupNames.LocationBlocks)]
    [ImageUrl("/icons/cms/blocks/map.png")]
    [AvailableContentTypes(Include = new Type[] { typeof(LocationListPage.LocationListPage) })]
    public class FilterDistancesBlock : FoundationBlockData, IFilterBlock
    {
        [CultureSpecific]
        [Display(Name = "Filter title")]
        public virtual string FilterTitle { get; set; }

        [CultureSpecific]
        [Display(Name = "All condition text")]
        public virtual string AllConditionText { get; set; }

        public ITypeSearch<LocationItemPage.LocationItemPage> AddFilter(ITypeSearch<LocationItemPage.LocationItemPage> query)
        {
            return query.GeoDistanceFacetFor(x => x.Coordinates, GeoPosition.GetUsersLocation().ToFindLocation(),
                new NumericRange { From = 0, To = 1000 },
                new NumericRange { From = 1000, To = 2500 },
                new NumericRange { From = 2500, To = 5000 },
                new NumericRange { From = 5000, To = 10000 },
                new NumericRange { From = 10000, To = 25000 });
        }

        public ITypeSearch<LocationItemPage.LocationItemPage> ApplyFilter(ITypeSearch<LocationItemPage.LocationItemPage> query, IQueryCollection filters)
        {
            var filterString = filters["d"];
            if (!string.IsNullOrWhiteSpace(filterString))
            {
                var stringDistances = filterString.ToList();
                if (stringDistances.Any())
                {
                    var userLocation = GeoPosition.GetUsersLocation().ToFindLocation();
                    var distances = ParseDistances(stringDistances);
                    var distancesFilter = SearchClient.Instance.BuildFilter<LocationItemPage.LocationItemPage>();
                    distancesFilter = distances.Aggregate(distancesFilter,
                                                          (current, distance) =>
                                                          current.Or(x => x.Coordinates.WithinDistanceFrom(
                                                              new GeoLocation(userLocation.Latitude,
                                                                              userLocation.Longitude),
                                                              ((int)distance.From.Value).Kilometers(),
                                                              ((int)distance.To.Value).Kilometers())));
                    query = query.Filter(x => distancesFilter);
                }
            }
            return query;
        }

        public static IEnumerable<NumericRange> ParseDistances(IEnumerable<string> distances)
        {
            if (distances == null)
            {
                yield break;
            }

            foreach (var distance in distances)
            {
                var distanceSplit = distance.Split('-');
                if (distanceSplit.Length == 2 && int.TryParse(distanceSplit[0], out var from) && int.TryParse(distanceSplit[1], out var to))
                {
                    yield return new NumericRange { From = from, To = to };
                }
            }
        }
    }
}