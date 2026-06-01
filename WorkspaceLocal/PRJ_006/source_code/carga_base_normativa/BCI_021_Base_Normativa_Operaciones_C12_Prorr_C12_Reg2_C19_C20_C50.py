# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_021_Base_Normativa_Operaciones_C12_Prorr_C12_Reg2_C19_C20_C50
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************************************************************************
# MAGIC * Nombre: BCI_021_Base_Normativa_Operaciones_C12_Prorr_C12_Reg2_C19_C20_C50.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones Activas C12 Prorrogas, C12 Registro 2, C19, C20 y C50
# MAGIC * Documentación: 
# MAGIC **************************************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_com_gr
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci21_basenorma_tmp_tipo_ope_12

# COMMAND ----------

drop_tipo_ope_12 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci21_basenorma_tmp_tipo_ope_12"""

# COMMAND ----------

sqlsafe(drop_tipo_ope_12)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci21_basenorma_tmp_tipo_ope_12", True)

# COMMAND ----------

create_tipo_ope_12 = """CREATE TABLE """ + db_plat_tempX + """.bci21_basenorma_tmp_tipo_ope_12
(
  periodo_id            INT,
  activo_id             INT,				
  cliente_rut           INT,
  cliente_dv            STRING,
  ope_vigente           STRING,
  ope_original          STRING,
  cod_cctb              STRING,
  cod_cartera_ope       STRING,
  cod_tipo_ope          STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci21_basenorma_tmp_tipo_ope_12' """

# COMMAND ----------

sqlsafe(create_tipo_ope_12)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci21_basenorma_tmp_c12_c19_c20_c50

# COMMAND ----------

drop_c12_c19_c20_c50 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50"""

# COMMAND ----------

sqlsafe(drop_c12_c19_c20_c50)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci21_basenorma_tmp_c12_c19_c20_c50", True)

# COMMAND ----------

create_c12_c19_c20_c50 = """CREATE TABLE """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
(
  periodo_id            INT,
  registro_id           INT,
  categoria             STRING,
  tipo_activo           INT,
  activo_id             INT,				
  cliente_rut           INT,
  cliente_dv            STRING,
  ope_vigente           STRING,
  ope_original          STRING,
  cod_cctb              STRING,
  cod_cartera_ope       STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci21_basenorma_tmp_c12_c19_c20_c50' """

# COMMAND ----------

sqlsafe(create_c12_c19_c20_c50)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci21_basenorma_tmp_v2

# COMMAND ----------

drop_tmp_v2 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci21_basenorma_tmp_v2"""

# COMMAND ----------

sqlsafe(drop_tmp_v2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci21_basenorma_tmp_v2", True)

# COMMAND ----------

create_tmp_v2 = """CREATE TABLE """ + db_plat_tempX + """.bci21_basenorma_tmp_v2
(
  periodo_id 							INT, 	
  num_registro            INT, 
  cod_categoria           STRING,
  num_tipo_activo  	      INT,		
  cod_activo 							STRING, 	
  rut_cliente 						INT, 	
  dv_cliente 							STRING, 	
  cod_num_operacion 			STRING,	  		
  cod_ope_original        STRING,   			
  cod_cta_contable        STRING,    			
  fec_proceso 						DATE 	

)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci21_basenorma_tmp_v2' """

# COMMAND ----------

sqlsafe(create_tmp_v2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 1: Obtencion Operaciones Covid Hipotecarias

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones activo 12 prorrogas hipotecarias por covid desde tmp_d00_deuda_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_tipo_ope_12 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00

# COMMAND ----------

vista_tipo_ope_12 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_tipo_ope_12
  select  periodo_id,
          12112               as activo_id,
          rut_cliente         as cliente_rut,	
          dv_cliente          as cliente_dv,			
          cod_num_operacion   as ope_vigente,
          cod_ope_original    as ope_original,
          cod_cctb,
          cod_cartera_ope,
          cod_tipo_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where trim(cod_cctb) in ('124604240', '124654240', '124611080', '124661080', '124691080', '141611127')
    and periodo_id = '""" + anomes + """'
    and trim(cod_cartera_ope) = 'ACT'
  union all
  select  periodo_id,
          12112               as activo_id,
          rut_cliente         as cliente_rut,	
          dv_cliente          as cliente_dv,			
          cod_num_operacion   as ope_vigente,
          cod_ope_original    as ope_original,
          cod_cctb,
          cod_cartera_ope,
          cod_tipo_ope
  from """ + db_plat_tempX + """.tmp_arm_d00_v3_act_3
  where periodo_id = '""" + anomes + """'
"""

# COMMAND ----------

sqlsafe(vista_tipo_ope_12)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 2: Obtencion Operaciones Tipo Activo 21

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del registro de cada operacion activo 12 para el C12 desde bci21_basenorma_tmp_activo_12 y v2_tabmae_mapeo_normativo
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_12 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  distinct
          a.periodo_id,
          b.registro_id,
          'C12'                   as categoria,
          12                      as tipo_activo,
          a.activo_id,
          a.cliente_rut,	
          a.cliente_dv,			
          a.ope_vigente,
          a.ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.bci21_basenorma_tmp_tipo_ope_12 a
  join """ + db_platinumX + """.tabmae_mapeo_normativo b
    on a.activo_id = b.mapeo_norma_id
"""
 

# COMMAND ----------

sqlsafe(vista_activo_12)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 3: Obtencion Operaciones Tipo Activo 52

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 52 para el C12 registro 2 desde tmp_d00_v3_act_ctg y v2_tabmae_mapeo_normativo
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_52 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          52                    as tipo_activo,
          12252                 as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_platinumX + """.tabmae_mapeo_normativo b
    on trim(a.cod_cctb) = trim(b.cod_cta_contable)
      and b.mapeo_norma_id = 12252
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'CTG'
"""


# COMMAND ----------

sql_safe(vista_activo_52)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 4: Obtencion Operaciones Tipo Activo 53

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 53 para el C12 registro 2 desde tmp_d00_v3_act_ctg y v2_tabmae_mapeo_normativo
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_53 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          53                    as tipo_activo,
          12253                 as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_platinumX + """.tabmae_mapeo_normativo b
    on trim(a.cod_cctb) = trim(b.cod_cta_contable)
      and b.mapeo_norma_id = 12253
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'CTG'
"""

# COMMAND ----------

sql_safe(vista_activo_53)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 5: Obtencion Operaciones Tipo Activo 54

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 54 para el C12 registro 2 desde tmp_d00_v3_act_ctg y v2_tabmae_mapeo_normativo
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_54 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          54                    as tipo_activo,
          12254                 as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_platinumX + """.tabmae_mapeo_normativo b
    on trim(a.cod_cctb) = trim(b.cod_cta_contable)
      and b.mapeo_norma_id = 12254
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'CTG'
"""


# COMMAND ----------

sql_safe(vista_activo_54)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 6: Obtencion Operaciones Tipo Activo 14, 15 y 16

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 14, 15 y 16 para el C19 registro 1 desde tmp_d00_v3_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_14_15_16 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  periodo_id,
          1                     as registro_id,
          'C19'                 as categoria,
          case when cod_tipo_activo = 14 then 14
               when cod_tipo_activo = 15 then 15
               when cod_tipo_activo = 16 then 16               
          end                   as tipo_activo,
          case when cod_tipo_activo = 14 then 19114
               when cod_tipo_activo = 15 then 19115
               when cod_tipo_activo = 16 then 19116               
          end                   as activo_id,
          rut_cliente           as cliente_rut,	
          dv_cliente            as cliente_dv,			
          cod_num_operacion     as ope_vigente,
          cod_ope_original      as ope_original,
          cod_cctb,
          cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where periodo_id = '""" + anomes + """'
    and cod_tipo_activo in (14, 15, 16)
    and trim(cod_calif) = 'GR'
    and trim(cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_14_15_16)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 7: Obtencion Operaciones Tipo Activo 61

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 61 para el C19 registro 2 desde tmp_d00_v3_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_61 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  periodo_id,
          2                     as registro_id,
          'C19'                 as categoria,
          61                    as tipo_activo,
          19261                 as activo_id,
          rut_cliente           as cliente_rut,	
          dv_cliente            as cliente_dv,			
          cod_num_operacion     as ope_vigente,
          cod_ope_original      as ope_original,
          cod_cctb,
          cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where periodo_id = '""" + anomes + """'
    and cod_tipo_activo = 61
    and trim(cod_calif) = 'GR'
    and trim(cod_cartera_ope) = 'CTG'
"""

# COMMAND ----------

sqlsafe(vista_activo_61)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 8: Obtencion Operaciones Tipo Activo 11 y 17

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 11 y 17 para el C20 registro 1 desde tmp_d00_v3_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_11_17 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  periodo_id,
          1                     as registro_id,
          'C20'                 as categoria,
          case when cod_tipo_activo = 11 then 11
               when cod_tipo_activo = 17 then 17             
          end                   as tipo_activo,
          case when cod_tipo_activo = 11 then 20111
               when cod_tipo_activo = 17 then 20117             
          end                   as activo_id,
          rut_cliente           as cliente_rut,	
          dv_cliente            as cliente_dv,			
          cod_num_operacion     as ope_vigente,
          cod_ope_original      as ope_original,
          cod_cctb,
          cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where periodo_id = '""" + anomes + """'
    and cod_tipo_activo in (11, 17)
    and trim(cod_calif) = 'GR'
    and trim(cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_11_17)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 9: Obtencion Operaciones Tipo Activo 41, 43, 44 y 51

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 41, 43, 44 y 51 para el C20 registro 2 desde tmp_d00_v3_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_41_43_44_51 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  periodo_id,
          2                     as registro_id,
          'C20'                 as categoria,
          case when cod_tipo_activo = 41 then 41
               when cod_tipo_activo = 43 then 43
               when cod_tipo_activo = 44 then 44
               when cod_tipo_activo = 51 then 51             
          end                   as tipo_activo,
          case when cod_tipo_activo = 41 then 20241
               when cod_tipo_activo = 43 then 20243
               when cod_tipo_activo = 44 then 20244
               when cod_tipo_activo = 51 then 20251             
          end                   as activo_id,
          rut_cliente           as cliente_rut,	
          dv_cliente            as cliente_dv,			
          cod_num_operacion     as ope_vigente,
          cod_ope_original      as ope_original,
          cod_cctb,
          cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where periodo_id = '""" + anomes + """'
    and cod_tipo_activo in (41, 43, 44, 51)
    and trim(cod_calif) = 'GR'
    and trim(cod_cartera_ope) = 'CTG'
"""

# COMMAND ----------

sqlsafe(vista_activo_41_43_44_51)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 10: Obtencion Operaciones Tipo Activo 11 C50

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones tipo activo 11 para el C50 registro 1 desde tmp_d00_v3_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_c12_c19_c20_c50 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_11_c50 = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
  select  periodo_id,
          1                     as registro_id,
          'C50'                 as categoria,
          11                    as tipo_activo,
          50111                 as activo_id,
          rut_cliente           as cliente_rut,	
          dv_cliente            as cliente_dv,			
          cod_num_operacion     as ope_vigente,
          cod_ope_original      as ope_original,
          cod_cctb,
          cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where periodo_id = '""" + anomes + """'
    and cod_tipo_activo = 11
    and trim(cod_cctb) = '111001500'
    and trim(cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_11_c50)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 11: Eliminacion Datos Tabla 

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v2_ft correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v2_ft (tabla de salida)

# COMMAND ----------

delete_base_normativa_v2_ft = """delete from """ + db_plat_tempX + """.base_normativa_v2_ft 
                                 where fec_proceso = '""" + date_proceso + """' and trim(cod_activo) in (12112, 12252, 12253, 12254, 19114, 19115, 19116, 19261,
                                                                                                         20111, 20117, 20241, 20243, 20244, 20251, 50111) """

# COMMAND ----------

sqlsafe(delete_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 12: Insercion Datos Tabla base_normativa_v2_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v2_ft desde la vista bci21_basenorma_tmp_c12_c19_c20_c50
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v2_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **num_registro**: Numero del tipo de registro	
# MAGIC - **cod_activo**: Codigo tipo activo				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente					
# MAGIC - **cod_num_operacion**: Número de la operación 
# MAGIC - **cod_ope_original**: Numero operación original
# MAGIC - **cod_cta_contable**: Cuenta contable
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v2_ft = """ insert into """ + db_plat_tempX + """.base_normativa_v2_ft
  select  periodo_id,
          registro_id                       as num_registro,
          categoria                         as cod_categoria,
          tipo_activo                       as num_tipo_activo,
          activo_id                         as cod_activo,
          cliente_rut                       as rut_cliente,	
          cliente_dv                        as dv_cliente,			
          ope_vigente                       as cod_num_operacion,	
          ope_original                      as cod_ope_original,	
          cod_cctb                          as cod_cta_contable,    
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci21_basenorma_tmp_c12_c19_c20_c50
"""

# COMMAND ----------

sqlsafe(insert_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 13: Obtencion Univ Clientes Duplicados

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los clientes duplicados desde base_normativa_v2_ft y tmp_operacion_duplicada_v3
# MAGIC
# MAGIC **Tabla de salida**: bci21_basenorma_tmp_v2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **num_registro**: Numero del tipo de registro	
# MAGIC - **cod_activo**: Codigo tipo activo				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente					
# MAGIC - **cod_num_operacion**: Número de la operación 
# MAGIC - **cod_ope_original**: Numero operación original
# MAGIC - **cod_cta_contable**: Cuenta contable
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

vista_cli_duplicados = """ insert into """ + db_plat_tempX + """.bci21_basenorma_tmp_v2
  select  distinct
          periodo_id,
          num_registro,
          cod_categoria,
          num_tipo_activo,
          cod_activo,
          rut_cliente,	
          dv_cliente,			
          cod_num_operacion,	
          cod_ope_original,	
          cod_cta_contable,    
          fec_proceso
  from """ + db_plat_tempX + """.base_normativa_v2_ft
  where fec_proceso = '""" + date_proceso + """'
    and trim(cod_num_operacion) in (select trim(cod_num_operacion) from """ + db_plat_tempX + """.tmp_operacion_duplicada_v3 where fec_proceso = '""" + date_proceso + """')
"""

# COMMAND ----------

sqlsafe(vista_cli_duplicados)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 14: Eliminacion Datos Tabla 

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v2_ft  correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v2_ft  (tabla de salida)

# COMMAND ----------

delete_base_normativa_v2_ft  = """delete from """ + db_plat_tempX + """.base_normativa_v2_ft 
                                  where fec_proceso = '""" + date_proceso + """' 
                                    and trim(cod_num_operacion) in (select trim(cod_num_operacion) from """ + db_plat_tempX + """.tmp_operacion_duplicada_v3 where fec_proceso = '""" + date_proceso + """')"""

# COMMAND ----------

sqlsafe(delete_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 15: Insercion Datos Tabla base_normativa_v2_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v2_ft desde la vista bci21_basenorma_tmp_v2
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v2_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **num_registro**: Numero del tipo de registro	
# MAGIC - **cod_activo**: Codigo tipo activo				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente					
# MAGIC - **cod_num_operacion**: Número de la operación 
# MAGIC - **cod_ope_original**: Numero operación original
# MAGIC - **cod_cta_contable**: Cuenta contable
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v2_ft = """ insert into """ + db_plat_tempX + """.base_normativa_v2_ft
  select  periodo_id,
          num_registro,
          cod_categoria,
          num_tipo_activo,
          cod_activo,
          rut_cliente,	
          dv_cliente,			
          cod_num_operacion,	
          cod_ope_original,	
          cod_cta_contable,    
          fec_proceso
  from """ + db_plat_tempX + """.bci21_basenorma_tmp_v2
  where fec_proceso = '""" + date_proceso + """'
"""

# COMMAND ----------

sqlsafe(insert_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 16: Actualizacion Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se realiza la actualizacion del campo cod_tabla_34 en la tabla base_normativa_v3_ft
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **cod_tabla_34**: Tipo Activo Créditos de consumo y de vivienda Tabla CMF 34

# COMMAND ----------

# MAGIC %md
# MAGIC ### Conteo de registros insertados en tabla de salida

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla base_normativa_v2_ft

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.base_normativa_v2_ft  where fec_proceso = '""" + date_proceso + """' and cod_activo in (12112, 12252, 12253, 12254, 19114, 19115, 19116, 19261, 20111, 20117, 20241, 20243, 20244, 20251, 50111) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_normativa_v2_ft")

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla base_normativa_v2_ft Duplicados

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.base_normativa_v2_ft  where fec_proceso = '""" + date_proceso + """' and cod_num_operacion in (select cod_num_operacion from """ + db_plat_tempX + """.tmp_operacion_duplicada_v3 where fec_proceso = '""" + date_proceso + """') """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_normativa_v2_ft")

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla base_normativa_v3_ft Actualizacion

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where fec_proceso = '""" + date_proceso + """' and cod_tabla_34 <> -998 """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Actualizaron " + str(cantidad) +" Registros en la tabla base_archivos_normativos")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")